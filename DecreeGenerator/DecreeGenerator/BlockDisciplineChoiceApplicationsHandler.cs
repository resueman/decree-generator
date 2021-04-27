using ApplicationsParser;
using ContingentParser;
using CurriculumParser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DecreeGenerator
{
    /// <summary>
    /// Позволяет обрабатывать заявления студентов на посещение дисциплин в элективном блоке. 
    /// Утверждает и отклоняет их, согласно пожеланиям обучающихся(приоритет дисциплины), нормативам, 
    /// указанным в рабочих программах дисциплин, успеваемости судентов. 
    /// Так же определяет, а затем распределяет никуда не записавшихся студентов, которые не ушли в академический отпуск.
    /// Работает как с блоками, которые доступны для всех студентов направления, так и с блоками, 
    /// выбирать дисциплины из которых могут только обучающиеся, имеющие определенную специализацию
    /// </summary>
    class BlockDisciplineChoiceApplicationsHandler
    {
        /// <summary>
        /// Элективный блок, заявления на дисциплины которого будут обрабатываться
        /// </summary>
        private readonly ElectivesBlock block;

        /// <summary>
        /// Студенты направления, которому принадлежит данный блок
        /// </summary>
        private readonly List<Student> students;

        /// <summary>
        /// Заявления на дисциплины определенного элективного блока
        /// </summary>
        private readonly List<DisciplineChoiceApplication> blockApplications;

        /// <summary>
        /// Нормативы для каждой дисциплины - минимально необходимое для открытия 
        /// и максимально возможное для посещения число студентов
        /// </summary>
        private readonly Dictionary<Discipline, (int Min, int Max)> normatives;

        /// <summary>
        /// Избыток студентов на каждой дисциплине, согласно нормативам
        /// overhead > 0 - необходимо убрать студентов в количестве = overhead
        /// overhead == 0 - добавлять студентов нельзя
        /// overhead < 0 - можно добавить студентов в количестве = -overhead
        /// </summary>
        private readonly Dictionary<Discipline, int> overheadForEachDiscipline;

        /// <summary>
        /// Недостаток студентов на каждой дисциплине, согласно нормативам
        /// required == normatives.Min ( > 0) - на дисциплину никто не зачислен
        /// required > 0 - необходимо добавить студентов в количестве = required
        /// required == 0 - отчисление студентов приведет к невозможности открыть дисциплину, следуя нормативам
        /// required < 0 - можно убирать студентов в количестве = -required
        /// </summary>
        private readonly Dictionary<Discipline, int> requiredForEachDiscipline;

        /// <summary>
        /// Создает экземляр класса <name>BlockDisciplineChoiceApplicationsHandler</name> для обработки 
        /// заявлений на выбор дисциплины в конкретном элективном блоке
        /// </summary>
        /// <param name="block">Элективный блок</param>
        /// <param name="students">Студенты из контингента и заявлений, обучающиеся на программе, 
        /// которой принадлежит данный элективный блок</param>
        /// <param name="blockApplications">Заявления на выбор дисциплин в данном элективном блоке</param>
        /// <param name="normatives">Нормативы с минимально необходимым и максимально 
        /// возможным числом зачисленных студентов на каждую дисциплину</param>
        public BlockDisciplineChoiceApplicationsHandler(ElectivesBlock block, List<Student> students,
            List<DisciplineChoiceApplication> blockApplications, Dictionary<Discipline, (int Min, int Max)> normatives)
        {
            this.block = block;
            this.students = students;
            this.normatives = normatives;
            this.blockApplications = blockApplications;

            overheadForEachDiscipline = new Dictionary<Discipline, int>();
            requiredForEachDiscipline = new Dictionary<Discipline, int>();
            foreach (var discipline in block.Disciplines.Select(p => p.Discipline))
            {
                overheadForEachDiscipline.Add(discipline, -normatives[discipline].Max);
                requiredForEachDiscipline.Add(discipline, normatives[discipline].Min);
            }
        }

        /// <summary>
        /// Обрабатывает заявки на дисциплины в элективном блоке
        /// </summary>
        /// <returns>Возвращает все заявления данного блока 
        /// после зачисления всех студентов на какую-либо дисциплину в блоке 
        /// и изменения статусов их заявлений</returns>
        public List<DisciplineChoiceApplication> HandleBlockApplications()
        {
            var firstPriorityApplications = blockApplications.Where(a => a.Priority == 1).ToArray();
            ApproveApplications(firstPriorityApplications);

            var expelledStudents = ExpellOverheadStudents();
            var studentsWhoHaveMissedFirstPriority = GetStudentsWhoHaveMissedFirstPriority();
            var rankingToLowerPrioritiesStudents = expelledStudents.Union(studentsWhoHaveMissedFirstPriority).ToList();
            DistributeStudentsByPrioritiesBelowTheFirst(rankingToLowerPrioritiesStudents);

            var enrolledStudents = blockApplications.Where(a => a.Status == Status.Approved).Select(a => a.Student).ToList();

            if (enrolledStudents.Count == 0)
            {
                var disciplines = block.Disciplines.Select(p => p.Discipline).ToList();
                DistributeNonEnrolledStudentsToClosedDisciplines(disciplines);
                return blockApplications;
            }

            var approvedStudentsForEachDiscipline = GetFilteredStudentsForEachDiscipline(a => a.Status == Status.Approved);

            var mathematicalExpectation = approvedStudentsForEachDiscipline.Sum(p => Math.Pow(p.Students.Count, 2)) / enrolledStudents.Count;
            var standardDeviation = Math.Sqrt(approvedStudentsForEachDiscipline.Sum(p => Math.Pow(p.Students.Count, 3)) / enrolledStudents.Count
                - Math.Pow(mathematicalExpectation, 2));

            var mostPopularDisciplines = approvedStudentsForEachDiscipline
                .Where(sd => sd.Students.Count >= mathematicalExpectation - standardDeviation)
                .Select(sd => sd.Discipline).ToList();

            var closedDisciplines = CloseDisciplinesWithNonLiquidatedShortageOfStudents();

            var nonEnrolledStudents = GetNonEnrolledStudents();
            if (nonEnrolledStudents.Count == 0)
            {
                return blockApplications;
            }

            ResolveRequiredWithNonEnrolledStudents();

            var openedDisciplines = block.Disciplines.Select(di => di.Discipline).Except(closedDisciplines).ToList();
            DistributeNonEnrolledStudentsToOpenedDisciplines(mostPopularDisciplines);
            DistributeNonEnrolledStudentsToOpenedDisciplines(openedDisciplines);
            DistributeNonEnrolledStudentsToClosedDisciplines(closedDisciplines);

            return blockApplications;
        }

        /// <summary>
        /// Утвердить заявку на дисциплину, пересчитать все показатели, связанные с элективом
        /// </summary>
        /// <param name="applications">Заявления на утверждение</param>
        private void ApproveApplications(params DisciplineChoiceApplication[] applications)
        {
            foreach (var application in applications)
            {
                application.Status = Status.Approved;
                ++overheadForEachDiscipline[application.Discipline];
                --requiredForEachDiscipline[application.Discipline];
            }
        } 

        /// <summary>
        /// Отлонить заявку на дисциплину, пересчитать все показатели, связанные с элективом
        /// </summary>
        /// <param name="applications">Заявления на отклонение</param>
        private void RejectApplications(params DisciplineChoiceApplication[] applications)
        {
            foreach (var application in applications)
            {
                application.Status = Status.Rejected;
                --overheadForEachDiscipline[application.Discipline];
                ++requiredForEachDiscipline[application.Discipline];
            }
        }

        /// <summary>
        /// Исключить студентов с дисциплин, на которые было зачислено количество учащихся, превышающее допустимое по нормативам.
        /// Отклоняются заявки студентов, имеющих наименьший средний балл
        /// </summary>
        /// <returns></returns>
        private List<Student> ExpellOverheadStudents()
        {
            var expelledSudents = new List<Student>();
            var disciplinesWithOverhead = overheadForEachDiscipline.Where(p => p.Value > 0).ToList();
            foreach (var (discipline, overhead) in disciplinesWithOverhead)
            {
                var expelled = blockApplications
                    .Where(a => a.Discipline == discipline && a.Status == Status.Approved)
                    .Select(a => a.Student)
                    .OrderBy(s => s.AverageScore)
                    .Take(overhead);

                expelledSudents.AddRange(expelled);
            }

            var applicationsToReject = new List<DisciplineChoiceApplication>();
            expelledSudents.ForEach(s => applicationsToReject.AddRange(blockApplications.Where(a => a.Student == s && a.Priority == 1)));
            RejectApplications(applicationsToReject.ToArray());

            return expelledSudents;
        }

        /// <summary>
        /// Определяет студентов, у которых отсутвует заявка на дисциплину в блоке с первым приоритетом,
        /// но присутствует заявка с каким-либо ненулевым приоритетом
        /// </summary>
        /// <returns>Список студентов, у которых есть заявки с каким-либо ненулевым приоритетом ниже первого</returns>
        private List<Student> GetStudentsWhoHaveMissedFirstPriority()
        {
            var studentsWhoHaveMissedFirstPriority = new List<Student>();
            var allAppliedStudents = blockApplications.Select(a => a.Student).ToList().Distinct().ToList();
            foreach (var student in allAppliedStudents)
            {
                var studentApplications = blockApplications.Where(a => a.Student == student && a.Priority > 0).ToList();
                if (studentApplications.All(a => a.Priority != 1))
                {
                    studentsWhoHaveMissedFirstPriority.Add(student);
                }
            }

            return studentsWhoHaveMissedFirstPriority;
        }

        /// <summary>
        /// Распределяет студентов по дисциплинам, приоритет заявок на которые ниже первого, 
        /// в соответсвии с их пожеланиями и нормативами, установленными для каждого электива.
        /// Зачисление обучающихся происходит в порядке с убывания среднего балла
        /// </summary>
        /// <param name="rankingStudents">Никуда не зачисленные студенты</param>
        private void DistributeStudentsByPrioritiesBelowTheFirst(List<Student> rankingStudents)
        {
            var studentsSortedByDescOfAverageScore = rankingStudents.OrderByDescending(s => s.AverageScore).ToList();
            foreach (var student in studentsSortedByDescOfAverageScore)
            {
                var studentApplicationsOrderedByPriority = blockApplications
                    .Where(a => a.Student == student && a.Priority > 1)
                    .OrderBy(a => a.Priority).ToList();

                foreach (var application in studentApplicationsOrderedByPriority)
                {
                    if (overheadForEachDiscipline[application.Discipline] < 0)
                    {
                        ApproveApplications(application);
                        break;
                    }
                    RejectApplications(application);
                }
            }
        }

        /// <summary>
        /// Закрываем дисциплины, недостаток людей на которых невозможно ликвидировать никуда не записавшимися студентами.
        /// Отклоняем утвержденные заявления, желающих посещать данные дисциплины.
        /// </summary>
        /// <returns>Список всех закрытых в блоке дисциплин</returns>
        private List<Discipline> CloseDisciplinesWithNonLiquidatedShortageOfStudents()
        {
            var approvedStudentsForEachDiscipline = 
                GetFilteredStudentsForEachDiscipline(a => a.Status == Status.Approved);

            var closingDisciplines = approvedStudentsForEachDiscipline
                .Where(p => p.Students.Count == 0).Select(p => p.Discipline).ToList();        

            var studentsForEachDiscipline = approvedStudentsForEachDiscipline
                .Where(p => p.Students.Count > 0).OrderBy(a => a.Students.Count).ToList();

            var totalRequired = CountTotalNumberOfRequiredStudents();
            var nonEnrolledStudentsCount = GetNonEnrolledStudents().Count;
            if (totalRequired > nonEnrolledStudentsCount)
            {
                foreach (var (closingDiscipline, students) in studentsForEachDiscipline)
                {
                    closingDisciplines.Add(closingDiscipline);
                    var rejectedApplications = blockApplications.Where(a => a.Discipline == closingDiscipline).ToArray();                    
                    RejectApplications(rejectedApplications);

                    foreach (var student in students.OrderByDescending(s=>s.AverageScore).ToList())
                    {
                        var applicationsSortedByIncreasingOfPriority = blockApplications
                            .Where(a => a.Student == student)
                            .Where(a => !closingDisciplines.Contains(a.Discipline))
                            .OrderBy(a => a.Priority).ToList();

                        foreach (var application in applicationsSortedByIncreasingOfPriority)
                        {
                            if (overheadForEachDiscipline[application.Discipline] < 0)
                            {
                                ApproveApplications(application);
                                break;
                            }
                            RejectApplications(application);
                        }
                    }

                    if (CountTotalNumberOfRequiredStudents() <= GetNonEnrolledStudents().Count)
                    {
                        break;
                    }
                }
            }

            return closingDisciplines;
        }

        /// <summary>
        /// Вычисляет, согласно нормативам, общее число студентов, необходимых для открытия всех дисциплин,
        /// на которые был зачислен хотя бы один человек
        /// </summary>
        /// <returns></returns>
        private int CountTotalNumberOfRequiredStudents()
        {
            var totalRequired = 0;
            foreach (var (discipline, required) in requiredForEachDiscipline)
            {
                if (required <= 0 || required == normatives[discipline].Min)
                {
                    continue;
                }
                totalRequired += required;
            }
            return totalRequired;
        }

        /// <summary>
        /// Определяет студентов, которые не зачислены ни на одну дисциплиину в блоке, а должны были
        /// </summary>
        /// <returns>Список студентов</returns>
        private List<Student> GetNonEnrolledStudents()
        {
            var enrolledStudents = blockApplications.Where(a => a.Status == Status.Approved)
                .Select(a => a.Student).ToList();

            var nonEnrolledStudents = students.Except(enrolledStudents).ToList();
            if (block.Specialization != null)
            {
                nonEnrolledStudents = nonEnrolledStudents.Where(s => s.Specialization == block.Specialization.Name).ToList();
            }

            nonEnrolledStudents.RemoveAll(s => s.Status == "прер");

            return nonEnrolledStudents;
        }

        /// <summary>
        /// Разрешаем проблему недостатка студенов для открытия всех дисциплин, на которые хоть кто-то был зачислен,
        /// с помощью никуда не записавшихся студентов.
        /// Необходимо, чтобы число никуда не записавшихся студентов было не менее общего числа недостающих студентов
        /// </summary>
        private void ResolveRequiredWithNonEnrolledStudents()
        {
            var sortedNonEnrolledStudents = GetNonEnrolledStudents().OrderBy(s => s.AverageScore).ToList();
            if (sortedNonEnrolledStudents.Count == 0)
            {
                return;
            }

            var disciplinesWithAShortageOfStudents = block.Disciplines.Select(di => di.Discipline)
                .Where(d => requiredForEachDiscipline[d] > 0 && requiredForEachDiscipline[d] != normatives[d].Min).ToList();

            foreach (var discipline in disciplinesWithAShortageOfStudents)
            {
                var required = requiredForEachDiscipline[discipline];
                var accepted = sortedNonEnrolledStudents.Take(required).ToList();
                sortedNonEnrolledStudents.RemoveRange(0, required);
                foreach (var student in accepted)
                {
                    var application = new DisciplineChoiceApplication(block, discipline, student, 0, Status.Approved, CommentType.StudentDidNotApplyForElectives);
                    ApproveApplications(application);
                    blockApplications.Add(application);
                }
            }
        }

        /// <summary>
        /// Распределяет, согласно нормативам, никуда не записавшихся студентов по открытым дисциплинам.
        /// Открытой дисциплиной считается та, на которую зачислили студентов.
        /// </summary>
        /// <param name="disciplines">Дисциплины, на которые хоть кто-то был зачислен</param>
        private void DistributeNonEnrolledStudentsToOpenedDisciplines(List<Discipline> disciplines)
        {
            var sortedNonEnrolledStudents = GetNonEnrolledStudents().OrderByDescending(s => s.AverageScore).ToList();
            disciplines = disciplines.Where(d => overheadForEachDiscipline[d] < 0).ToList();
            if (sortedNonEnrolledStudents.Count == 0 || disciplines.Count == 0)
            {
                return;
            }

            var chunkSize = (int)Math.Ceiling((double)sortedNonEnrolledStudents.Count / disciplines.Count);
            foreach (var discipline in disciplines)
            {
                if (sortedNonEnrolledStudents.Count == 0)
                {
                    return;
                }
                var toAccept = chunkSize <= -overheadForEachDiscipline[discipline] ? chunkSize : -overheadForEachDiscipline[discipline];
                var studentsToAccept = sortedNonEnrolledStudents.Take(toAccept).ToList();
                sortedNonEnrolledStudents.RemoveRange(0, studentsToAccept.Count);
                foreach (var student in studentsToAccept)
                {
                    var application = new DisciplineChoiceApplication(block, discipline, student, 0, Status.Approved, CommentType.StudentDidNotApplyForElectives);
                    ApproveApplications(application);
                    blockApplications.Add(application);
                }
            }
        }

        /// <summary>
        /// Распределяет, согласно нормативам, никуда не зачисленных студентов по закрытым дисциплинам.
        /// Закытая дисциплина - дисциплина, на которую нет желающих => никто не был зачислен
        /// </summary>
        /// <param name="disciplines">Дисциплины, на которые не был зачислен ни один студент</param>
        private void DistributeNonEnrolledStudentsToClosedDisciplines(List<Discipline> disciplines) // вот это переделать
        {
            disciplines = disciplines.Where(d => requiredForEachDiscipline[d] == normatives[d].Min).ToList();
            var nonEnrolledStudents = GetNonEnrolledStudents();
            if (nonEnrolledStudents.Count == 0)
            {
                return;
            }

            var studentsForEachOpenedDiscipline = new List<(Discipline Discipline, int StudentsCount)>();
            disciplines = disciplines.OrderByDescending(d => normatives[d].Min + normatives[d].Max).ToList();
            foreach (var discipline in disciplines)
            {
                if (nonEnrolledStudents.Count == 0)
                {
                    return;
                }
                if (normatives[discipline].Min > nonEnrolledStudents.Count)
                {
                    continue;
                }
                var toTake = (normatives[discipline].Min + normatives[discipline].Max) / 2;
                toTake = toTake >= nonEnrolledStudents.Count ? nonEnrolledStudents.Count : toTake;
                var approvedStudents = nonEnrolledStudents.Take(toTake).ToList();
                nonEnrolledStudents.RemoveRange(0, approvedStudents.Count);
                studentsForEachOpenedDiscipline.Add((discipline, approvedStudents.Count));
                foreach (var student in approvedStudents)
                {
                    var application = new DisciplineChoiceApplication(block, discipline, student, 0, Status.New, CommentType.StudentDidNotApplyForElectives);
                    ApproveApplications(application);
                    blockApplications.Add(application);
                }
            }

            var studentsByOpenedDisciplines = studentsForEachOpenedDiscipline.Where(sd => sd.StudentsCount > 0).ToList();
            var closed = studentsForEachOpenedDiscipline.Where(sd => sd.StudentsCount == 0).Select(sd => sd.Discipline).ToList();
            var remaining = studentsForEachOpenedDiscipline.Where(sd => sd.StudentsCount > 0).Sum(sd => normatives[sd.Discipline].Max - sd.StudentsCount);
            while (remaining < nonEnrolledStudents.Count)
            {
                var discipline = closed.OrderBy(d => normatives[d].Min).First();
                var students = nonEnrolledStudents.Take((normatives[discipline].Min + normatives[discipline].Max) / 2).ToList();
                nonEnrolledStudents.RemoveRange(0, students.Count);
                foreach (var student in students)
                {
                    var application = new DisciplineChoiceApplication(block, discipline, student, 0, Status.Approved, CommentType.StudentDidNotApplyForElectives);
                    ApproveApplications(application);
                    blockApplications.Add(application);
                }
                remaining = studentsForEachOpenedDiscipline.Where(sd => sd.StudentsCount > 0).Sum(sd => normatives[sd.Discipline].Max - sd.StudentsCount);
            }

            while (nonEnrolledStudents.Count != 0)
            {
                var disciplineRequiredForStudent = studentsForEachOpenedDiscipline.FirstOrDefault(sd => normatives[sd.Discipline].Min > sd.StudentsCount);
                var discipline = disciplineRequiredForStudent == default
                    ? studentsByOpenedDisciplines.First(ds => ds.StudentsCount < normatives[ds.Discipline].Max).Discipline
                    : disciplineRequiredForStudent.Discipline;

                var student = nonEnrolledStudents[0];
                nonEnrolledStudents.RemoveAt(0);
                var application = new DisciplineChoiceApplication(block, discipline, student, 0, Status.Approved, CommentType.StudentDidNotApplyForElectives);
                ApproveApplications(application);
                blockApplications.Add(application);
            }
        }

        /// <summary>
        /// Для каждой дисциплины определяет подававшихся на нее студентов, заявки которых удовлетворяют предикату 
        /// </summary>
        /// <param name="predicate">Функция для проверки каждой заявки на условие</param>
        /// <returns>Пары из дисциплины и студентов, подававшихся на нее, заявки которых удовлетворяют предикату</returns>
        private List<(Discipline Discipline, List<Student> Students)> GetFilteredStudentsForEachDiscipline(Func<DisciplineChoiceApplication, bool> predicate)
        {
            var studentsForEachDiscipline = new List<(Discipline Discipline, List<Student> Students)>();
            var disciplines = block.Disciplines.Select(p => p.Discipline);
            foreach (var discipline in disciplines)
            {
                var students = blockApplications
                    .Where(a => a.Discipline == discipline)
                    .Where(predicate)
                    .Select(a => a.Student)
                    .ToList();

                studentsForEachDiscipline.Add((discipline, students));
            }

            return studentsForEachDiscipline;
        }
    }
}

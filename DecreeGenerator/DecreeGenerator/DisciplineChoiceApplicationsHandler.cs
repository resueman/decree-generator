using ApplicationsParser;
using ContingentParser;
using CurriculumParser;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DecreeGenerator
{
    /// <summary>
    /// Позволяет обрабатывать заявления студентов на посещение дисциплин, 
    /// согласно пожеланиям обучающихся и нормативам, указанным в рабочих программах дисциплин. 
    /// Так же определяет, а затем распределяет никуда не записавшихся студентов, которые не ушли в академический отпуск.
    /// Работает как с блоками, которые доступны для всех студентов направления, 
    /// так и с блоками, выбирать дисциплины из которых могут только обучающиеся, имеющие определенную специализацию.
    /// Предоставляет результат работы в виде списка студентов, зачисленных на каждую дисциплину, с разбиением по элективным блокам
    /// </summary>
    public class DisciplineChoiceApplicationsHandler
    {
        /// <summary>
        /// Студенты определенного направления из контингента и из списка с заявлениями
        /// </summary>
        private readonly List<Student> students;
        
        /// <summary>
        /// Учебный план, у которго имеются элективные блоки
        /// </summary>
        private readonly ICurriculumWithElectiveBlocks curriculum;
        
        /// <summary>
        /// Заявления студентов о выборе дисциплин, в которых нет подозрительных или неправильных данных
        /// </summary>
        private readonly List<DisciplineChoiceApplication> correctApplications;

        /// <summary>
        /// Заявления с подозрительными или неправильными данными
        /// </summary>
        private readonly List<DisciplineChoiceApplication> applicationsWithCorruptedData;

        /// <summary>
        /// Нормативы для каждой дисциплины - минимально необходимое для открытия
        /// и максимально возможное для посещения число студентов
        /// </summary>
        private readonly Dictionary<Discipline, (int Min, int Max)> normatives;

        /// <summary>
        /// Распределение студентов по дисциплинам с разбиением по элективным блокам после утверждения и отклонения заявлений
        /// </summary>
        public Dictionary<ElectivesBlock, Dictionary<Discipline, List<(Student, CommentType)>>> Distribution { get; private set; }

        public List<(Student, CommentType)> NotProcesedStudents { get; private set; }

        /// <summary>
        /// Создает экземляр класса <name>DisciplineChoiceApplicationsHandler</name> для обработки заявлений на выбор дисциплин, 
        /// принадлежащих любому элективному блоку
        /// </summary>
        public DisciplineChoiceApplicationsHandler()
        {
            Distribution = new Dictionary<ElectivesBlock, Dictionary<Discipline, List<(Student, CommentType)>>>();
            NotProcesedStudents = new List<(Student, CommentType)>();
        }

        /// <summary>
        /// Создает экземляр класса <name>DisciplineChoiceApplicationsHandler</name> для обработки заявлений на выбор дисциплин, 
        /// Определяет распределение студентов по элективам на основе уже утвержденных заявлений, 
        /// т.е. с учетом статуса, полученного при парсинге заявлений
        /// </summary>
        /// <param name="applications">Заявления студентов</param>
        public DisciplineChoiceApplicationsHandler(DisciplineChoiceApplications applications)
            : this()
        {
            correctApplications = applications.ToList();
            InitializeDistributionWithApprovedApplications();
        }

        /// <summary>
        /// Создает экземляр класса <name>DisciplineChoiceApplicationsHandler</name> для обработки заявлений на выбор дисциплин. 
        /// Создает распределение студентов по элективам с помощью алгоритма распределения по блокам, но 
        /// с учетом статуса, полученного при парсинге заявлений
        /// </summary>
        /// <param name="curriculum">Учебный план, у которого есть элективные блоки</param>
        /// <param name="contingent">Контингент(студенты)</param>
        /// <param name="applications">Заявления студентов о выборе дисциплин</param>
        /// <param name="normatives">Нормативы с минимально необходимым и максимально 
        /// возможным числом зачисленных студентов на каждую дисциплину</param>
        public DisciplineChoiceApplicationsHandler(ICurriculumWithElectiveBlocks curriculum, Contingent contingent, 
            DisciplineChoiceApplications applications, Dictionary<Discipline, (int Min, int Max)> normatives)
            : this()
        {
            applicationsWithCorruptedData = applications.Where(a => a.Comment != CommentType.Ok).ToList();
            applicationsWithCorruptedData.ForEach(a => NotProcesedStudents.Add((a.Student, a.Comment)));

            correctApplications = applications.Where(a => a.Priority > 0 && a.Student.Status != "прер" && a.Comment == CommentType.Ok).ToList();

            this.curriculum = curriculum;
            this.normatives = normatives;

            var studentsFromContingent = contingent.Where(s => AreEqualCodes(s.CurriculumCode, curriculum.CurriculumCode)).ToList();
            // var studentsFromApplicationsNotInContingent = applications.Select(a => a.Student).Distinct().Except(studentsFromContingent).ToList();
            students = studentsFromContingent.Where(s => s.Status != "прер").ToList();

            foreach (var d in curriculum.Disciplines.Distinct())
            {
                if (normatives.ContainsKey(d))
                {
                    continue;
                }
                normatives.Add(d, (1, 100));
            }

            DistributeStudentsToElectives();

            InitializeDistributionWithApprovedApplications();
        }

        /// <summary>
        /// Определяет равны ли коды учебных учебных планов, вне зависимости от типа разделителя
        /// </summary>
        /// <param name="code1">Код учебного плана</param>
        /// <param name="code2">Код учебного плана</param>
        /// <returns>True, если коды учебных планов равны, иначе false</returns>
        private bool AreEqualCodes(string code1, string code2) => ClearCode(code1) == ClearCode(code2);

        /// <summary>
        /// Убирает разделители из кода учебного плана
        /// </summary>
        /// <param name="code">Код учебного плана</param>
        /// <returns>Код учебного плана без разделителя</returns>
        private string ClearCode(string code) => code.Replace("/", "").Replace("\\", "");

        /// <summary>
        /// Распределяет студентов по элективам с помощью обработки заявлений каждого блока
        /// </summary>
        private void DistributeStudentsToElectives()
        {
            var blocks = curriculum.ElectiveBlocks.Where(b => b.Disciplines.Count > 1).ToList();
            var processedBlockApplicationsTasks = new List<Task<List<DisciplineChoiceApplication>>>();
            foreach (var block in blocks)
            {
                var blockApplications = correctApplications.Where(a => a.ElectivesBlock == block).ToList();
                var processedBlockApplicationsTask = Task.Run(
                    () => new BlockDisciplineChoiceApplicationsHandler(block, students, blockApplications, normatives).HandleBlockApplications());
                processedBlockApplicationsTasks.Add(processedBlockApplicationsTask);
            }
            correctApplications.Clear();
            Task.WaitAll(processedBlockApplicationsTasks.ToArray());
            foreach (var processedBlockApplicationsTask in processedBlockApplicationsTasks)
            {
                correctApplications.AddRange(processedBlockApplicationsTask.Result);
            }
        }

        /// <summary>
        /// Инициализирует структуру данных, отвечающую за распределение, с помощью информации из утвержденных заявок
        /// </summary>
        private void InitializeDistributionWithApprovedApplications()
        {
            var blockDisciplinePairs = correctApplications.Select(a => (a.ElectivesBlock, a.Discipline)).Distinct().ToList();
            var blocks = blockDisciplinePairs.Select(bd => bd.ElectivesBlock).Distinct().ToList();
            foreach (var block in blocks)
            {
                var disciplines = blockDisciplinePairs.Where(bd => bd.ElectivesBlock == block).Select(bd => bd.Discipline);
                var studentsByDiscipline = new Dictionary<Discipline, List<(Student Student, CommentType Comment)>>();
                var encounteredUniqueStudents = new HashSet<Student>();
                foreach (var discipline in disciplines)
                {
                    studentsByDiscipline.Add(discipline, new List<(Student, CommentType)>());

                    var approvedApplicationsForDiscipline = correctApplications
                        .Where(a => a.ElectivesBlock == block && a.Discipline == discipline && a.Status == Status.Approved)
                        .ToList();

                    foreach (var a in approvedApplicationsForDiscipline)
                    {
                        if (encounteredUniqueStudents.Contains(a.Student))
                        {
                            var (d, list) = studentsByDiscipline.Single(p => (default, default) != p.Value.SingleOrDefault(p => p.Student == a.Student));
                            var (s, c) = list.Single(s => s.Student == a.Student);
                            studentsByDiscipline[d].Remove((s, c));
                            studentsByDiscipline[d].Add((s, CommentType.StudentHasAlreadyAppearedInBlock));
                        }

                        var comment = encounteredUniqueStudents.Contains(a.Student)
                            ? CommentType.StudentHasAlreadyAppearedInBlock
                            : a.Comment;

                        encounteredUniqueStudents.Add(a.Student);

                        studentsByDiscipline[discipline].Add((a.Student, comment));
                    }
                }
                Distribution.Add(block, studentsByDiscipline);
            }
        }
    }
}

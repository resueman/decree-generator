using ContingentParser;
using CurriculumParser;
using System.Collections;
using System.Collections.Generic;

namespace ApplicationsParser
{
    /// <summary>
    /// Содержит заявления о выборе дисциплин, логику их парсинга
    /// </summary>
    public class DisciplineChoiceApplications : Applications, IEnumerable<DisciplineChoiceApplication>
    {
        private readonly List<DisciplineChoiceApplication> applications;

        /// <summary>
        /// Создает экземляр класса <name>DisciplineChoiceApplications</name>
        /// </summary>
        /// <param name="fileName">Имя файла с расширением .docx с заявлениями о выборе дисциплин</param>
        /// <param name="semester">Заявления какого семестра хотим получить</param>
        /// <param name="curriculum">Учебный план, заявления учащихся которого хотим обрабатывать</param>
        /// <param name="contingent">Контингент - все студенты факультета</param>
        public DisciplineChoiceApplications(string fileName, int semester,
            ICurriculumWithElectiveBlocks curriculum, Contingent contingent)
            : base(fileName, "ФИО", curriculum, contingent, 4, semester)
        {
            applications = new List<DisciplineChoiceApplication>();
            ParseApplicationsFile();
            if (applications.Count == 0)
            {
                throw new ApplicationParsingException("Не найдено ни одного заявления, соответсвующего данному учебному плану. " +
                    "Проверьте файлы с заявлениями и учебным планом");
            }
        }

        /// <summary>
        /// Парсит приоритет заявления по стоке, содержащей его
        /// </summary>
        /// <param name="line">Строка содержащая значение приоритета дисциплины из заявления</param>
        /// <returns>Приоритет</returns>
        private protected int ParsePriority(string line)
        {
            if (int.TryParse(line, out int priority))
            {
                return priority;
            }
            throw new ApplicationParsingException($"Недопустимое значение '{line}' в колонке 'Приоритет'. " +
                $"Проверьте файл с заявлениями на соответсвие установленному формату");
        }

        /// <summary>
        /// Парсит заявления блока(не обязательно элективного)
        /// </summary>
        /// <param name="blockInfo">Строка с информацией о блоке</param>
        /// <param name="currentClearedCurriculumCode">Код учебного плана без разделителя из файла с заявлениями, заявления которого парсим</param>
        public override void ParseBlockApplications(string blockInfo, string currentClearedCurriculumCode)
        {
            if (!TryParseInfoAboutElectiveBlock(blockInfo, currentClearedCurriculumCode, out int semester, out int blockNumber,
                out string specializationName, out int applicationsCount) || semester != this.semester)
            {
                return;
            }

            var applicationsInfo = new List<(Discipline Discipline, Student Student, int Priority, Status Status, CommentType Comment)>();
            var encounteredDisciplines = new HashSet<Discipline>();
            for (var i = 0; i < applicationsCount; ++i)
            {
                var student = ParseStudent(GetNextLine());
                var disciplineName = GetNextLine();
                var priority = ParsePriority(GetNextLine());
                var status = ParseStatus(GetNextLine()); status = Status.New; ////////

                if (!TryParseDiscipline(disciplineName, out var discipline))
                {
                    var application = new DisciplineChoiceApplication(null, 
                        new Discipline("", disciplineName, "", DisciplineType.Elective),
                        student, default, Status.New, CommentType.StudentAppliedForElectiveLackingInCurriculum);

                    applications.Add(application);
                    continue;
                }

                var comment = ClearCurriculumCode(student.CurriculumCode) != currentClearedCurriculumCode
                    ? CommentType.StudentEnrolledInDifferentCurriculumJudgingByTheContingent
                    : studentsExistedInApplicationsNotInContingent.Contains(student)
                    ? CommentType.AppliedStudentIsNotListedInContingent
                    : CommentType.Ok;
                
                if (comment != CommentType.Ok)
                {
                    var application = new DisciplineChoiceApplication(null, discipline, student, priority, status, comment);
                    applications.Add(application);
                    continue;
                }

                encounteredDisciplines.Add(discipline);
                applicationsInfo.Add((discipline, student, priority, status, CommentType.Ok));
            }

            var electivesBlock = DetermineElectiveBlock(specializationName, encounteredDisciplines);

            foreach (var a in applicationsInfo)
            {
                var application = new DisciplineChoiceApplication(electivesBlock,
                    a.Discipline, a.Student, a.Priority, a.Status, a.Comment);

                applications.Add(application);
            }
        }

        /// <summary>
        /// Возвращает перечислитель, который проходит через заявления о выборе 
        /// перечислимого экземляра класса <name>DisciplineChoiceApplication</name>
        /// </summary>
        /// <returns>Перечислитель</returns>
        public IEnumerator<DisciplineChoiceApplication> GetEnumerator() 
            => applications.GetEnumerator();

        /// <summary>
        /// Возвращает перечислитель, который проходит через заявления о выборе 
        /// перечислимого экземляра класса <name>DisciplineChoiceApplication</name>
        /// </summary>
        /// <returns>Перечислитель</returns>
        IEnumerator IEnumerable.GetEnumerator() 
            => applications.GetEnumerator();
    }
}
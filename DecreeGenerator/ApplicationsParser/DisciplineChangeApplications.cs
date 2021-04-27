using ContingentParser;
using CurriculumParser;
using System.Collections;
using System.Collections.Generic;

namespace ApplicationsParser
{
    /// <summary>
    /// Содержит заявления о перевыборе заявления, логику их парсинга
    /// </summary>
    public class DisciplineChangeApplications : Applications, IEnumerable<DisciplineChangeApplication>
    {
        private readonly List<DisciplineChangeApplication> applications;

        /// <summary>
        /// Создает экземляр класса <name>DisciplineChangeApplications</name>
        /// </summary>
        /// <param name="fileName">Имя файла с расширением .docx с заявлениями о выборе дисциплин</param>
        /// <param name="semester">Заявления какого семестра хотим получить</param>
        /// <param name="curriculum">Учебный план, заявления учащихся которого хотим обрабатывать</param>
        /// <param name="contingent">Контингент - все студенты факультета</param>
        public DisciplineChangeApplications(string fileName,
            int semester, ICurriculumWithElectiveBlocks curriculum, Contingent contingent) 
            : base(fileName, "Прежняя дисциплина", curriculum, contingent, 4, semester)
        {
            applications = new List<DisciplineChangeApplication>();
            ParseApplicationsFile();
            if (applications.Count == 0)
            {
                throw new ApplicationParsingException("Не найдено ни одного заявления, соответсвующего данному учебному плану. " +
                    "Проверьте файлы с заявлениями и учебным планом");
            }
        }

        /// <summary>
        /// Парсит заявления блока(не обязательно элективного)
        /// </summary>
        /// <param name="blockInfo">Строка с информацией о блоке</param>
        /// <param name="currentClearedCurriculumCode">Код учебного плана без разделителя из файла с заявлениями, заявления которого парсим</param>
        public override void ParseBlockApplications(string blockInfo, string currentClearedCurriculumCode, string specialization)
        {
            if (!TryParseInfoAboutElectiveBlock(blockInfo, currentClearedCurriculumCode, specialization, out int semester, out int blockNumber,
                   out string specializationName, out int applicationsCount) || semester != this.semester)
            {
                return;
            }

            var applicationsInfo = new List<(Discipline InitialDiscipline, Discipline FinalDiscipline, Student Student, Status Status, CommentType Comment)>();
            var encounteredDisciplines = new HashSet<Discipline>();
            for (var i = 0; i < applicationsCount; ++i)
            {
                var initialDisciplineName = GetNextLine();
                var finalDisciplineName = GetNextLine();
                var student = ParseStudent(GetNextLine());
                var status = ParseStatus(GetNextLine());

                if (!TryParseDiscipline(initialDisciplineName, out var initialDiscipline) 
                    || !TryParseDiscipline(finalDisciplineName, out var finalDiscipline))
                {
                    var application = new DisciplineChangeApplication(null, 
                        new Discipline("", initialDisciplineName, "", DisciplineType.Elective),
                        new Discipline("", finalDisciplineName, "", DisciplineType.Elective), 
                        student, status, CommentType.StudentAppliedForElectiveLackingInCurriculum);

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
                    var application = new DisciplineChangeApplication(null, initialDiscipline, finalDiscipline, student, status, comment);
                    applications.Add(application);
                    continue;
                }

                encounteredDisciplines.Add(initialDiscipline);
                encounteredDisciplines.Add(finalDiscipline);
                applicationsInfo.Add((initialDiscipline, finalDiscipline, student, status, CommentType.Ok));
            }

            var electivesBlock = DetermineElectiveBlock(specializationName, encounteredDisciplines);
            foreach (var a in applicationsInfo)
            {
                var application = new DisciplineChangeApplication(electivesBlock,
                    a.InitialDiscipline, a.FinalDiscipline, a.Student, a.Status, a.Comment);

                applications.Add(application);
            }
        }

        /// <summary>
        /// Возвращает перечислитель, который проходит через заявления о перевыборе 
        /// перечислимого экземляра класса <name>DisciplineChangeApplications</name>
        /// </summary>
        /// <returns>Перечислитель</returns>
        public IEnumerator<DisciplineChangeApplication> GetEnumerator()
            => applications.GetEnumerator();

        /// <summary>
        /// Возвращает перечислитель, который проходит через заявления о перевыборе 
        /// перечислимого экземляра класса <name>DisciplineChangeApplications</name>
        /// </summary>
        /// <returns>Перечислитель</returns>
        IEnumerator IEnumerable.GetEnumerator()
            => applications.GetEnumerator();
    }
}
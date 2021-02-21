using ApplicationsParser;
using ContingentParser;
using CurriculumParser;
using System.Collections.Generic;
using System.Linq;

namespace DecreeGenerator
{
    /// <summary>
    /// Позволяет обрабатывать заявления учащихся на перевыбор дисциплин, с учетом нормативов и успеваемости студентов.
    /// </summary>
    public class DisciplineChangeApplicationsHandler
    {
        /// <summary>
        /// Заявления на перевыбор дисциплины
        /// </summary>
        private readonly List<DisciplineChangeApplication> applications;

        /// <summary>
        /// Нормативы для каждой дисциплины - минимально необходимое для открытия
        /// и максимально возможное для посещения число студентов
        /// </summary>
        private readonly Dictionary<Discipline, (int Min, int Max)> normatives;

        /// <summary>
        /// Первоначальное "правильное" распределение, полученное на основе пожеланий студентов, с учетом их успеваимости и нормативами 
        /// </summary>
        private readonly Dictionary<ElectivesBlock, Dictionary<Discipline, List<(Student, CommentType)>>> initialDistribution;

        /// <summary>
        /// Финальное "правильное" распределение, полученное на основе заявлений о перевыборе, нармативов, успеваемости студентов
        /// </summary>
        public Dictionary<ElectivesBlock, Dictionary<Discipline, List<(Student, CommentType)>>> Distribution { get; private set; }

        public List<(Student, CommentType)> NotProcesedStudents { get; private set; }

        /// <summary>
        /// Создает экземляр класса <name>DisciplineChangeApplicationsHandler</name> для обработки заявлений на выбор дисциплин. 
        /// Создает распределение студентов по элективам на основе заявлений о перевыборе дисциплин
        /// </summary>
        /// <param name="initialDistribution">Первоначальное "правильное" распределение, полученное на основе заявок о выборе дисциплин</param>
        /// <param name="applications">Заявления студентов о перевыборе дисциплин</param>
        /// <param name="normatives">Нормативы с минимально необходимым и максимально 
        /// возможным числом зачисленных студентов на каждую дисциплину</param>
        public DisciplineChangeApplicationsHandler(Dictionary<ElectivesBlock, Dictionary<Discipline, List<(Student, CommentType)>>> initialDistribution,
            DisciplineChangeApplications applications, Dictionary<Discipline, (int, int)> normatives)
        {
            NotProcesedStudents = new List<(Student, CommentType)>();
            applications.Where(a => a.Comment != CommentType.Ok).ToList().ForEach(a => NotProcesedStudents.Add((a.Student, a.Comment)));

            this.initialDistribution = initialDistribution;
            this.applications = applications.Where(a => a.Comment == CommentType.Ok).ToList();
            this.normatives = normatives;

            Distribution = new Dictionary<ElectivesBlock, Dictionary<Discipline, List<(Student, CommentType)>>>();

            RedistributeStudentsToElectives();
        }

        /// <summary>
        /// Обрабатывает заявления, пинадлежащие конкретному элективному блоку, на перевыбор дисциплин
        /// </summary>
        private void RedistributeStudentsToElectives()
        {
            foreach (var block in initialDistribution.Keys)
            {
                var blockApplications = applications.Where(a => a.ElectivesBlock == block).ToList();

                var studentsForEachDisciplineAfterRedistribution = new BlockDisciplineChangeApplicationsHandler(block,
                    blockApplications, normatives, initialDistribution[block]).HandleBlockApplications();

                var result = new Dictionary<Discipline, List<(Student, CommentType)>>();
                foreach (var (discipline, students) in studentsForEachDisciplineAfterRedistribution)
                {
                    result.Add(discipline, new List<(Student, CommentType)>());
                    foreach(var (student, comment) in students)
                    {
                        result[discipline].Add((student, comment));
                    }
                }

                Distribution.Add(block, result);
            }
        }
    }
}

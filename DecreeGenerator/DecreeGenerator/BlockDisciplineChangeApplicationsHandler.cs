using ApplicationsParser;
using ContingentParser;
using CurriculumParser;
using System.Collections.Generic;
using System.Linq;

namespace DecreeGenerator
{
    /// <summary>
    /// Обрабатывает заявления учащихся на перевыбор дисциплины в конкретном элективном блоке. 
    /// Утверждает и отклоняет заявления на основе нормативов, указанных в рабочих программах дисциплин и успеваемости судентов.
    /// Работает на основе "правильного" распределения студентов по элективам, полученного после обработки заявлений с приоритетами.
    /// На выходе так же получается "правильное распределение" - распределение, в котором учтены нормативы и, насколько возможно,
    /// пожелания обучающихся
    /// </summary>
    class BlockDisciplineChangeApplicationsHandler
    {
        /// <summary>
        /// Заявления на дисциплины определенного элективного блока
        /// </summary>
        private readonly List<DisciplineChangeApplication> blockApplications;

        /// <summary>
        /// Нормативы для каждой дисциплины - минимально необходимое для открытия 
        /// и максимально возможное для посещения число студентов
        /// </summary>
        private readonly Dictionary<Discipline, (int Min, int Max)> normatives;
        
        /// <summary>
        /// Распределение студентов по дисциплинам
        /// </summary>
        private readonly Dictionary<Discipline, List<(Student, CommentType)>> distributionInBlock;

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
        /// Создает экземляр класса<name> BlockDisciplineChangeApplicationsHandler</name> для обработки
        /// заявлений на перевыбор дисциплины в конкретном элективном блоке
        /// </summary>
        /// <param name="block">Элективный блок, заявления на дисиплины которого необходимо обработать</param>
        /// <param name="blockApplications">Заявления студентов на перевыбор дисциплин в конкретном элективном блоке</param>
        /// <param name="normatives">Нормативы с минимально необходимым и максимально 
        /// возможным числом зачисленных студентов на каждую дисциплину</param>
        /// <param name="distributionInBlock">Распределение студентов, полученное после этапа выбора, с учетом нормативов, академщиков, незаписавшихся</param>
        public BlockDisciplineChangeApplicationsHandler(ElectivesBlock block, List<DisciplineChangeApplication> blockApplications, 
            Dictionary<Discipline, (int Min, int Max)> normatives, Dictionary<Discipline, List<(Student, CommentType)>> distributionInBlock)
        {
            this.normatives = normatives;
            this.blockApplications = blockApplications;
            this.distributionInBlock = distributionInBlock;

            overheadForEachDiscipline = new Dictionary<Discipline, int>();
            requiredForEachDiscipline = new Dictionary<Discipline, int>();
            foreach (var discipline in block.Disciplines.Select(p => p.Discipline))
            {
                overheadForEachDiscipline.Add(discipline, -normatives[discipline].Max);
                requiredForEachDiscipline.Add(discipline, normatives[discipline].Min);
            }
        }

        /// <summary>
        /// Обрабатывает заявления студентов на перевыбор дисциплин в конкретном элективном блоке 
        /// на основе уже существующего, "правильного" распределения
        /// </summary>
        /// <returns>Финальное распределение студентов по дисциплинам в конкретном элективном блоке, 
        /// после обработки заявлений на перевыбор дисциплины</returns>
        public Dictionary<Discipline, List<(Student, CommentType)>> HandleBlockApplications()
        {
            ApproveApplications(blockApplications.ToArray());

            ResolveRequired();

            if (overheadForEachDiscipline.All(dov => dov.Value <= 0))
            {
                return distributionInBlock;
            }            

            ResolveOverhead();

            TryToMaximizeTheNumberOfApprovedApplications();

            return distributionInBlock;
        }

        /// <summary>
        /// Отклоняет одобренные заявки на перевыбор дисциплин наименее успешных студентов,
        /// переход которых на другую дисциплину привел к недобору студентов согласно нормативам
        /// </summary>
        private void ResolveRequired()
        {
            var (disciplineWithShortfall, required) = requiredForEachDiscipline
                .Where(dr => dr.Value > 0 && dr.Value != normatives[dr.Key].Max)
                .OrderBy(dr => dr.Value)
                .FirstOrDefault();

            while (disciplineWithShortfall != null)
            {
                var leftStudentsApplications = blockApplications
                    .Where(a => a.InitialDiscipline == disciplineWithShortfall)
                    .OrderBy(a => a.Student.AverageScore)
                    .ToList();

                var rejectingApplications = leftStudentsApplications
                    .Take(normatives[disciplineWithShortfall].Min - required)
                    .ToArray();

                RejectApplications(rejectingApplications);

                (disciplineWithShortfall, required) = requiredForEachDiscipline
                .Where(dr => dr.Value > 0 && dr.Value != normatives[dr.Key].Max)
                .OrderBy(dr => dr.Value)
                .FirstOrDefault();
            }
        }

        /// <summary>
        /// Отклоняет одобренные заявки на перевыбор дисциплин наименее успешных студентов, 
        /// если дисциплину желает посещать больше студентов, чем разрешено нормативами
        /// </summary>
        private void ResolveOverhead()
        {
            var (disciplineWithOverhead, overhead) = requiredForEachDiscipline
                .Where(dr => dr.Value > 0).OrderByDescending(dr => dr.Value).FirstOrDefault();

            while (disciplineWithOverhead != null)
            {
                var arrivedStudentsApplications = blockApplications
                    .Where(a => a.FinalDiscipline == disciplineWithOverhead)
                    .OrderBy(a => a.Student.AverageScore).ToList();

                var rejectingApplications = arrivedStudentsApplications
                    .Take(arrivedStudentsApplications.Count - normatives[disciplineWithOverhead].Max)
                    .ToArray();

                RejectApplications(rejectingApplications);

                (disciplineWithOverhead, overhead) = requiredForEachDiscipline
                .Where(dr => dr.Value > 0).OrderByDescending(dr => dr.Value).FirstOrDefault();
            }
        }

        /// <summary>
        /// После этапов разрешения недобора и перебора студентов, пытается, с учетом нормативов и успеваемости,
        /// утвердить отклоненные заявления наиболее успешных студентов, если это возможно
        /// </summary>
        private void TryToMaximizeTheNumberOfApprovedApplications()
        {
            var rejectedApplications = blockApplications
                .Where(c => c.Status == Status.Rejected)
                .OrderByDescending(c => c.Student.AverageScore);

            foreach (var application in rejectedApplications)
            {
                var final = application.FinalDiscipline;
                var initial = application.InitialDiscipline;
                if (distributionInBlock[final].Count + 1 <= normatives[final].Max
                    && distributionInBlock[initial].Count - 1 >= normatives[initial].Min)
                {
                    application.Status = Status.Approved;
                    distributionInBlock[initial].Remove((application.Student, application.Comment));
                    distributionInBlock[final].Add((application.Student, application.Comment));
                }
            }
        }

        /// <summary>
        /// Утвердить заявку на дисциплину, пересчитать все показатели, связанные с элективом
        /// </summary>
        /// <param name="applications">Заявления на утверждение</param>
        private void ApproveApplications(params DisciplineChangeApplication[] applications)
        {
            foreach (var a in applications)
            {

                distributionInBlock[a.InitialDiscipline].Remove((a.Student, a.Comment));
                distributionInBlock[a.FinalDiscipline].Add((a.Student, a.Comment));
                a.Status = Status.Approved;
                --overheadForEachDiscipline[a.InitialDiscipline];
                ++requiredForEachDiscipline[a.InitialDiscipline];
                ++overheadForEachDiscipline[a.FinalDiscipline];
                --overheadForEachDiscipline[a.InitialDiscipline];
            }
        }

        /// <summary>
        /// Отлонить заявку на дисциплину, пересчитать все показатели, связанные с элективом
        /// </summary>
        /// <param name="applications">Заявления на отклонение</param>
        private void RejectApplications(params DisciplineChangeApplication[] applications)
        {
            foreach (var a in applications)
            {
                distributionInBlock[a.InitialDiscipline].Add((a.Student, a.Comment));
                distributionInBlock[a.FinalDiscipline].Remove((a.Student, a.Comment));
                a.Status = Status.Approved;
                ++overheadForEachDiscipline[a.InitialDiscipline];
                --requiredForEachDiscipline[a.InitialDiscipline];
                --overheadForEachDiscipline[a.FinalDiscipline];
                ++overheadForEachDiscipline[a.InitialDiscipline];
            }
        }
    }
}

using ApplicationsParser;
using ContingentParser;
using CurriculumParser;
using DecreeGenerator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DecreeGeneratorUI
{
    /// <summary>
    /// Model - часть реализации паттерна MVVM.
    /// Содержит логику создания файла со списком студентов на основе поступающих от пользователя данных. 
    /// </summary>
    public class GeneratorModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Создает экземпляр класса GeneratorModel
        /// </summary>
        public GeneratorModel()
        {
            locker = new object();
        }

        private int progress;
        private readonly object locker;

        /// <summary>
        /// Идентифицирует на сколько % готов приказ
        /// </summary>
        public int ProgressValue
        {
            get => progress;
            set
            {
                progress = value;
                OnPropertyChanged(nameof(ProgressValue));
            }
        }

        /// <summary>
        /// Содержит логику создания файла со списком студентов на основе поступающих от пользователя данных. 
        /// </summary>
        /// <param name="semester">Семестр</param>
        /// <param name="contingentFileName">Названия файла с контингентом</param>
        /// <param name="curriculumFileName">Название файла с учебным планом</param>
        /// <param name="disciplineChoiceApplicationsFileName">Название файла с заявлениями о выборе дисциплин</param>
        /// <param name="disciplineChangeApplicationsFileName">Название файла с заявлениями о перевыборе дисциплин</param>
        /// <param name="isDisciplineChangeOption">Идентифицирует, обработку заявлений какого процесса воспроизводим - выбор или перевыбор</param>
        /// <returns>щбъект класса Task</returns>
        public async Task GenerateDecree(string semester, string contingentFileName, string curriculumFileName,
            string disciplineChoiceApplicationsFileName, string disciplineChangeApplicationsFileName, 
            bool isDisciplineChangeOption)
        {
            try
            {
                await GenerateDecreeTask(semester, contingentFileName, curriculumFileName, 
                    disciplineChoiceApplicationsFileName, disciplineChangeApplicationsFileName, 
                    isDisciplineChangeOption);

                ProgressValue = 0;
            }
            catch (Exception e)
            {
                ProgressValue = 0;
                throw e;
            }
        }

        private Task GenerateDecreeTask(string semester, string contingentFileName, string curriculumFileName,
            string disciplineChoiceApplicationsFileName, string disciplineChangeApplicationsFileName,
            bool isDisciplineChangeOption)
        {
            var progressChunk = isDisciplineChangeOption ? 100 / 7 : 20;

            var contingentTask = Task.Run(() =>
            {
                var contingent = new Contingent(contingentFileName);
                IncreaseProgressValue(progressChunk);
                return contingent;
            });

            var curriculumTask = Task.Run(() =>
            {
                var extension = Path.GetExtension(curriculumFileName);
                var curriculum = extension == ".txt"
                    ? new TxtCurriculum(curriculumFileName)
                    : new DocxCurriculum(curriculumFileName)
                        as ICurriculumWithElectiveBlocks;

                IncreaseProgressValue(progressChunk);
                return curriculum;
            });

            return Task.Run(async () =>
            {
                await contingentTask;
                await curriculumTask;

                var contingent = contingentTask.Result;
                var curriculum = curriculumTask.Result;

                var disciplineChoiceApplications = new DisciplineChoiceApplications(
                    disciplineChoiceApplicationsFileName,
                    int.Parse(semester), curriculum, contingent);

                IncreaseProgressValue(progressChunk);

                var normatives = GetNormatives(disciplineChoiceApplications);

                var disciplineChoiceApplicationsHandler = isDisciplineChangeOption
                    ? new DisciplineChoiceApplicationsHandler(disciplineChoiceApplications)
                    : new DisciplineChoiceApplicationsHandler(curriculum, contingent, disciplineChoiceApplications, normatives);

                IncreaseProgressValue(progressChunk);

                var code = curriculum.CurriculumCode.Split('\\', '/')[1];
                var annexFileName = isDisciplineChangeOption
                    ? $"Приложение {code} перевыбор семестр {semester}.docx"
                    : $"Приложение {code} выбор семестр {semester}.docx";
                
                if (isDisciplineChangeOption)
                {
                    var changedChoices = new DisciplineChangeApplications(
                        disciplineChangeApplicationsFileName, int.Parse(semester), curriculum, contingent);

                    IncreaseProgressValue(progressChunk);

                    var initialDistribution = disciplineChoiceApplicationsHandler.Distribution;
                    var handler = new DisciplineChangeApplicationsHandler(initialDistribution, changedChoices, normatives);

                    IncreaseProgressValue(progressChunk);

                    DecreeFileGenerator.GenerateDecree(annexFileName, handler.NotProcesedStudents, handler.Distribution);

                    IncreaseProgressValue(progressChunk);
                    return;
                }

                DecreeFileGenerator.GenerateDecree(annexFileName, 
                    disciplineChoiceApplicationsHandler.NotProcesedStudents, 
                    disciplineChoiceApplicationsHandler.Distribution);

                IncreaseProgressValue(progressChunk);
            });
        }

        private void IncreaseProgressValue(int progressChunk)
        {
            lock (locker)
            {
                ProgressValue += progressChunk;
            }
        }

        private Dictionary<Discipline, (int Min, int Max)> GetNormatives(DisciplineChoiceApplications applications)
        {
            var normatives = new Dictionary<Discipline, (int Min, int Max)>();
            foreach (var d in applications.Select(a => a.Discipline).Distinct())
            {
                normatives.Add(d, (1, 100));
            }
            return normatives;
        }

        /// <summary>
        /// Событие изменения свойства
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Вызывается в случае изменения свойства, оповещает подписавшихся на свойство об его изменении
        /// </summary>
        /// <param name="PropertyName">Имя свойства</param>
        public void OnPropertyChanged([CallerMemberName] string PropertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }
    }
}

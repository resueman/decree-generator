using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DecreeGeneratorUI
{
    /// <summary>
    /// Пока что тут происходит что-то страшное >_< и нужны комменты
    /// </summary>
    class GeneratorViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private readonly GeneratorModel generatorModel;

        private bool? isDisciplineChangeOption;
        private string contingentFileName;
        private string curriculumFileName;
        private string disciplineChoiceApplicationsFileName;
        private string disciplineChangeApplicationsFileName;
        private string semester;

        private bool isReady = true;
        private bool areAllFieldsValid;

        private bool isInputClearing;

        public GeneratorViewModel()
        {
            generatorModel = new GeneratorModel();
            generatorModel.PropertyChanged += (s, e) => { OnPropertyChanged(e.PropertyName); };
        }

        /// <summary>
        /// плохо
        /// </summary>
        public bool HasAnyOptionChosen
        {
            get => isReady && IsDisciplineChangeOption != null;
            set
            {
                OnPropertyChanged(nameof(HasAnyOptionChosen));
            }
        }

        /// <summary>
        /// плохо
        /// </summary>
        public bool IsDisciplineChangedChoiceButtonEnabled
        {
            get => isReady && IsDisciplineChangeOption == true;
            set
            {
                OnPropertyChanged(nameof(IsDisciplineChangedChoiceButtonEnabled));
            }
        }

        /// <summary>
        /// неоч
        /// </summary>
        public bool IsPossibleToGenerateNewDecree
        {
            get => isReady && areAllFieldsValid;
            set
            {   
                isReady = value;
                OnPropertyChanged(nameof(IsPossibleToGenerateNewDecree));
            }
        }

        /// <summary>
        /// aaaaaaaaaaaaaaaaaaaaaaaaaaaa
        /// </summary>
        public bool? IsDisciplineChangeOption
        {
            get => isDisciplineChangeOption;
            set
            {
                isDisciplineChangeOption = value;
                OnPropertyChanged(nameof(IsDisciplineChangeOption));
                OnPropertyChanged(nameof(HasAnyOptionChosen));
                OnPropertyChanged(nameof(IsDisciplineChangedChoiceButtonEnabled));
                areAllFieldsValid = AreAllFieldsValid();
                OnPropertyChanged(nameof(IsPossibleToGenerateNewDecree));
                if (isDisciplineChangeOption == false)
                {
                    isInputClearing = true;
                    DisciplineChangeApplicationsFileName = "";
                    isInputClearing = false;
                }
            }
        }

        public string Semester
        {
            get => semester;
            set
            {
                semester = value;
                OnPropertyChanged(nameof(Semester));                
            }
        }

        public string ContingentFileName
        {
            get => contingentFileName;
            set
            {
                contingentFileName = value;
                OnPropertyChanged(nameof(ContingentFileName));
            }
        }

        public string CurriculumFileName
        {
            get => curriculumFileName;
            set
            {
                curriculumFileName = value;
                OnPropertyChanged(nameof(CurriculumFileName));
            }
        }

        public string DisciplineChoiceApplicationsFileName
        {
            get => disciplineChoiceApplicationsFileName;
            set
            {
                disciplineChoiceApplicationsFileName = value;
                OnPropertyChanged(nameof(DisciplineChoiceApplicationsFileName));
            }
        }

        public string DisciplineChangeApplicationsFileName
        {
            get => disciplineChangeApplicationsFileName;
            set
            {
                disciplineChangeApplicationsFileName = value;
                OnPropertyChanged(nameof(DisciplineChangeApplicationsFileName));
            }
        }

        public int ProgressValue => generatorModel.ProgressValue;

        public async Task GenerateDecree()
        {
            try
            {
                IsPossibleToGenerateNewDecree = false;
                await generatorModel.GenerateDecree(Semester, ContingentFileName, CurriculumFileName,
                    DisciplineChoiceApplicationsFileName, DisciplineChangeApplicationsFileName,
                    (bool)IsDisciplineChangeOption);
                ClearInput();
            }
            catch (Exception e)
            {
                ClearInput();
                throw e;
            }
        }

        private void ClearInput()
        {
            isInputClearing = true;
            Semester = "";
            CurriculumFileName = "";
            DisciplineChoiceApplicationsFileName = "";
            DisciplineChangeApplicationsFileName = "";
            isInputClearing = false;
            areAllFieldsValid = false;
            IsPossibleToGenerateNewDecree = true;
        }

        public string Error => throw new NotImplementedException();

        public string this[string name]
        {
            get
            {
                string result = null;
                if (isInputClearing)
                {
                    return result;
                }
                switch (name)
                {
                    case nameof(IsDisciplineChangeOption):
                        if (IsDisciplineChangeOption == false)
                        {
                            DisciplineChangeApplicationsFileName = DisciplineChangeApplicationsFileName;
                            return null;
                        }
                        break;
                    case nameof(Semester):
                        if (Semester != null && (!int.TryParse(Semester, out var value) || value < 1 || value > 10))
                        {
                            result = "Допустимые значения семестра от 1 до 10";
                        }
                        break;
                    case nameof(ContingentFileName):
                        if (ContingentFileName == "")
                        {
                            result = "Выберите файл";
                        }
                        break;
                    case nameof(CurriculumFileName):
                        if (CurriculumFileName == "")
                        {
                            result = "Выберите файл";
                        }
                        break;
                    case nameof(DisciplineChoiceApplicationsFileName):
                        if (DisciplineChoiceApplicationsFileName == "")
                        {
                            result = "Выберите файл";
                        }
                        break;
                    case nameof(DisciplineChangeApplicationsFileName):
                        if (DisciplineChangeApplicationsFileName == "")
                        {
                            result = "Выберите файл";
                        }
                        break;
                }
                areAllFieldsValid = AreAllFieldsValid();
                OnPropertyChanged(nameof(IsPossibleToGenerateNewDecree));
                return result;
            }
        }

        public bool AreAllFieldsValid()
        {
            if (IsDisciplineChangeOption == null || string.IsNullOrEmpty(contingentFileName)
                || string.IsNullOrEmpty(curriculumFileName) || string.IsNullOrEmpty(DisciplineChoiceApplicationsFileName)
                || !int.TryParse(Semester, out var intSemester) || intSemester < 1 || intSemester > 10)
            {
                return false;
            }
            return (IsDisciplineChangeOption == false || !string.IsNullOrEmpty(DisciplineChangeApplicationsFileName)) && semester != null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string PropertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }
    }
}

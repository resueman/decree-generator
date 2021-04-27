namespace ContingentParser
{
    /// <summary>
    /// Реализует сущность "Студент", которая содержит основную информацию об обучающемся
    /// </summary>
    public class Student
    {
        /// <summary>
        /// Инициализирует сущность <name>Student</name>
        /// </summary>
        /// <param name="surname">Фамилия</param>
        /// <param name="name">Имя</param>
        /// <param name="patronymic">Отчество</param>
        /// <param name="levelOfEducation">Уровень обучения (бакалавриат/магистратура...)</param>
        /// <param name="programmeCode">Код программы</param>
        /// <param name="curriculumCode">Код учебного плана</param>
        /// <param name="status">Статус - "студ"(учится)/"прер"(находится  в академическом отпуске)</param>
        /// <param name="specialization">Специализация/кафедра</param>
        /// <param name="course">Текущий курс</param>
        /// <param name="groupInContingent">Группа, указанная в файле с контингентом</param>
        /// <param name="yearOfAdmission">Год поступления</param>
        /// <param name="averageScore">Средний балл</param>
        /// <param name="educationState">Состояние учебы - "норм"/"хвост"(наличие долгов)</param>
        public Student(string surname, string name, string patronymic,
            string levelOfEducation, string programmeCode, string curriculumCode, string status,
            string specialization = "", string course = "", string groupInContingent = "", 
            string yearOfAdmission = "", string averageScore = "", string educationState = "") 
        {
            Surname = surname;
            Name = name;
            Patronymic = patronymic;
            FullName = $"{surname} {name} {patronymic}";

            LevelOfStudy = levelOfEducation;
            ProgrammeCode = programmeCode;
            CurriculumCode = curriculumCode;
            Specialization = specialization;
            Course = course;
            GroupInContingent = groupInContingent;
            YearOfAdmission = yearOfAdmission;
            AverageScore = averageScore;
            Status = status;
            EducationState = educationState;
        }

        /// <summary>
        /// Фамилия
        /// </summary>
        public string Surname { get; private set; }

        /// <summary>
        /// Имя
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Отчество
        /// </summary>
        public string Patronymic { get; private set; }

        /// <summary>
        /// ФИО
        /// </summary>
        public string FullName { get; private set; }

        /// <summary>
        /// Уровень обучения - бакалавриат/магистратура/...
        /// </summary>
        public string LevelOfStudy { get; private set; }

        /// <summary>
        /// Код программы, на которой учится студент
        /// </summary>
        public string ProgrammeCode { get; private set; }
        
        /// <summary>
        /// Код учебного плана, по которому обучается студент
        /// </summary>
        public string CurriculumCode { get; private set; }

        /// <summary>
        /// Специализация/кафедра
        /// </summary>
        public string Specialization { get; private set; }

        /// <summary>
        /// Курс
        /// </summary>
        public string Course { get; private set; }

        /// <summary>
        /// Группа, в которой обучается студент, указанная в файле с контингентом
        /// </summary>
        public string GroupInContingent { get; private set; }

        /// <summary>
        /// Год поступления
        /// </summary>
        public string YearOfAdmission { get; private set; }

        /// <summary>
        /// Средний балл
        /// </summary>
        public string AverageScore { get; private set; }

        /// <summary>
        /// Идентифицирует, учится(статус "студ") ли студент в данный момент или находится в академическом отпуске(статус "прер") 
        /// </summary>
        public string Status { get; private set; }

        /// <summary>
        /// Состояние учебы "норм", "хвост"(есть долги)
        /// </summary>
        public string EducationState { get; private set; }
    }
}

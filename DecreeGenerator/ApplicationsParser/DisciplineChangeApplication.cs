using ContingentParser;
using CurriculumParser;

namespace ApplicationsParser
{
    /// <summary>
    /// Представляет сущность "Заявление на изменение дисциплины"
    /// </summary>
    /// <remarks>Содержит информацию о студенте, дисциплине, на которую изначально он был зачислен,
    /// дисциплине, которую он хочет посещать в итоге, 
    /// элективном блоке, в рамках которого осуществляется переход, стаутсе заявления</remarks>
    public class DisciplineChangeApplication
    {
        /// <summary>
        /// Создает экземпляр класса <name>DisciplineChangeApplication</name>
        /// </summary>
        /// <param name="electivesBlock">Элективный блок</param>
        /// <param name="initialDiscipline">Дисциплина, которую студент не хочет посещать</param>
        /// <param name="finalDiscipline">Дисциплина, которую студент хочет посещать</param>
        /// <param name="student">Студент-автор заявления</param>
        /// <param name="status">Статус заявления</param>
        public DisciplineChangeApplication(ElectivesBlock electivesBlock, Discipline initialDiscipline, 
            Discipline finalDiscipline, Student student, Status status, CommentType comment)
        {
            ElectivesBlock = electivesBlock;
            InitialDiscipline = initialDiscipline;
            FinalDiscipline = finalDiscipline;
            Student = student;
            Status = status;
            Comment = comment;
        }

        /// <summary>
        /// Элективный блок, в рамках которого студент меняет дисциплины
        /// </summary>
        public ElectivesBlock ElectivesBlock { get; private set; }

        /// <summary>
        /// Дисциплина, на которую студент был зачислен изначально, но с которой желает уйти
        /// </summary>
        public Discipline InitialDiscipline { get; private set; }

        /// <summary>
        /// Электив, на который студент хочет сменить дисциплину, на которую был определен изначально
        /// </summary>
        public Discipline FinalDiscipline { get; private set; }
        
        /// <summary>
        /// Студент-автор заявления
        /// </summary>
        public Student Student { get; private set; }

        /// <summary>
        /// Статус заявления
        /// </summary>
        public Status Status { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public CommentType Comment { get; set; }
    }
}

using ContingentParser;
using CurriculumParser;

namespace ApplicationsParser
{
    /// <summary>
    /// Сущность "Заявление на выбор дисциплины"
    /// </summary>
    /// <remarks>Содержит информацию о студенте, дисциплине, приоритете, ей присвоенном,
    /// элективном блоке, которому она принадлежит и статусе заявления</remarks>
    public class DisciplineChoiceApplication
    {
        /// <summary>
        /// Создает экземляр класса <name>DisciplineChoiceApplication</name>
        /// </summary>
        /// <param name="electivesBlock">Элективный блок</param>
        /// <param name="discipline">Дисциплина</param>
        /// <param name="student">Студент</param>
        /// <param name="priority">Приоритет, присвоенный дисциплине</param>
        /// <param name="status">Статус заявления</param>
        public DisciplineChoiceApplication(ElectivesBlock electivesBlock, Discipline discipline, 
            Student student, int priority, Status status, CommentType comment)
        {
            ElectivesBlock = electivesBlock;
            Discipline = discipline;
            Student = student;
            Priority = priority;
            Status = status;
            Comment = comment;
        }

        /// <summary>
        /// Элективный блок, которому принадлежит дисциплина, которую хочет посещать студент
        /// </summary>
        public ElectivesBlock ElectivesBlock { get; private set; }

        /// <summary>
        /// Дисциплина для посещения
        /// </summary>
        public Discipline Discipline { get; private set; }

        /// <summary>
        /// Студент, которому принадлежит заявление
        /// </summary>
        public Student Student { get; private set; }
        
        /// <summary>
        /// Приоритет, присвоенный студентом данной дисциплине. 
        /// Принимает значения от 0 до N, где N - количество дисциплин в элективном блоке.
        /// 0 - студент просматривал дисциплину, но не присвоил ей приоритет.
        /// 1 - наиболее желаемая дисциплина для изучения. N - наименее желаемая
        /// </summary>
        public int Priority { get; private set; }

        /// <summary>
        /// Статус заявления
        /// </summary>
        public Status Status { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public CommentType Comment { get; private set; }
    }
}

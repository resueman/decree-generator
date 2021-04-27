using ContingentParser;
using CurriculumParser;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ApplicationsParser
{
    /// <summary>
    /// Содержит методы парсинга файлов с заявлениями студентов о выборе и перевыборе дисциплин
    /// </summary>
    public abstract class Applications 
    {
        private protected readonly int semester;
        private readonly Contingent contingent;
        private readonly string clearedCurriculumCode;
        private protected readonly ICurriculumWithElectiveBlocks curriculum;

        private readonly OpenXmlReader reader;
        private readonly string firstHeaderColumnName;
        private readonly int applicationFileColumnsCount;

        private protected readonly HashSet<Student> studentsExistedInApplicationsNotInContingent;

        /// <summary>
        /// Создает экземпляр класса <name>ApplicationsParser</name>
        /// </summary>
        /// <param name="fileName">Имя файла с расширением .docx, содержащего заявления студентов</param>
        /// <param name="firstHeadersColumnName">Название первого столбца шапки таблицы с заявлениями</param>
        /// <param name="curriculum">Учебный план</param>
        /// <param name="contingent">Контингент</param>
        /// <param name="columnsCount">Количество непустых параграфов, содержащих информацию о заявлении</param>
        /// <param name="semester">Заявки какого семестра хотим получить</param>
        public Applications(string fileName, string firstHeadersColumnName,
            ICurriculumWithElectiveBlocks curriculum, Contingent contingent, int columnsCount, int semester)
        {
            this.semester = semester;
            this.contingent = contingent;
            this.curriculum = curriculum;
            firstHeaderColumnName = firstHeadersColumnName;

            applicationFileColumnsCount = columnsCount;
            clearedCurriculumCode = ClearCurriculumCode(curriculum.CurriculumCode);
            studentsExistedInApplicationsNotInContingent = new HashSet<Student>();

            var wordDocument = WordprocessingDocument.Open(fileName, false);
            var body = wordDocument.MainDocumentPart.Document.Body; 
            var paragraph = body.Elements<Paragraph>();
            reader = OpenXmlReader.Create(body);
        }

        /// <summary>
        /// Возвращает следующую непустую строку
        /// </summary>
        /// <returns>Возвращает следующую непустую строку</returns>
        private protected string GetNextLine()
        {
            var text = "";
            while (reader.Read() && text == "")
            {
                if (reader.ElementType == typeof(Text))
                {
                    text = reader.GetText();
                }
                if (text == firstHeaderColumnName)
                {
                    for (var i = 0; i < applicationFileColumnsCount; ++i)
                    {
                        text = GetNextLine();
                    }
                }
            }
            return text;
        }

        /// <summary>
        /// Анализирует строки файла с заявлением и запускает соответсвующие методы их парсинга
        /// </summary>
        private protected void ParseApplicationsFile()
        {
            string currentClearedCurriculumCode = "";
            while (reader.Read())
            {
                var line = GetNextLine();
                var match = Regex.Match(line, @"([^:]+):\s+(.+)");
                if (!match.Success)
                {
                    continue;
                }

                switch (match.Groups[1].Value)
                {
                    case "Учебный план":
                        currentClearedCurriculumCode = ParseClearedCurriculumCode(line);
                        continue;
                    case "Блок учебного плана":
                        try
                        {
                            ParseBlockApplications(match.Groups[2].Value, currentClearedCurriculumCode);
                        }
                        catch (Exception e)
                        {
                            throw new ApplicationParsingException($"При парсинге заявлений в блоке {match.Groups[2].Value} " +
                                $"учебного плана {currentClearedCurriculumCode} произошла ошибка", e);
                        }
                        continue;
                }
            }
        }

        /// <summary>
        /// Содержит логику обработки заявлений, принадлежащих одному блоку(не обязательно элективном), в зависимости от типа заявлений(выбор или перевыбор)
        /// </summary>
        /// <param name="blockInfo">Строка, содержащая информацию блоке(не обязательно элективном)</param>
        /// <param name="currentClearedCurriculumCode">Код учебного плана без разделителей</param>
        public abstract void ParseBlockApplications(string blockInfo, string currentClearedCurriculumCode);

        /// <summary>
        /// Убирает '/', '\' разделители из кода учебного плана
        /// </summary>
        /// <param name="code">Код учебного плана</param>
        /// <returns>Код учебного плана без разделителей</returns>
        private protected string ClearCurriculumCode(string code)
            => code.Replace("/", "").Replace(@"\", "").Replace(" ", "");

        /// <summary>
        /// Находит код учебного плана в строке и убирвет все разделители в нем
        /// </summary>
        /// <param name="line">Строка, содержащая код учебного плана</param>
        /// <returns>Код учебного плана без разделителей</returns>
        private string ParseClearedCurriculumCode(string line)
        {
            line = line.Replace("/", "").Replace(@"\", "");
            var curriculumMatch = Regex.Match(line, @"Учебный план:\s+(\d{7})\s+([^(]+)");
            if (curriculumMatch.Success)
            {
                return curriculumMatch.Groups[1].Value;
            }
            return default;
        }

        /// <summary>
        /// Определяет статус заявления по передаваемой строке
        /// </summary>
        /// <param name="status">Строка содержащащая статус заявления</param>
        /// <returns>Статус заявления</returns>
        private protected Status ParseStatus(string status)
        {
            return status switch
            {
                "Новая" => Status.New,
                "Утверждена" => Status.Approved,
                "Отклонена" => Status.Rejected,
                _ => throw new Exception($"Недопустимое значение '{status}' в колонке 'Статус'. " +
                $"Проверьте файл с заявлениями на соответсвие установленному формату")
            };
        }

        /// <summary>
        /// По ФИО определяет, какой студент из контингента подал заявление. 
        /// Если такого студента не обнаружено, создаем его
        /// </summary>
        /// <param name="studentName">ФИО студента</param>
        /// <returns>Экземляр класса <name>Student</name></returns>
        private protected Student ParseStudent(string studentName)
        {
            Student student;
            try
            {
                student = contingent.Single(s => s.FullName == studentName
                    && ClearCurriculumCode(s.CurriculumCode) == clearedCurriculumCode);
            }
            catch (InvalidOperationException)
            {
                try
                {
                    student = contingent.Single(s => s.FullName == studentName);
                }
                catch (InvalidOperationException)
                {
                    student = studentsExistedInApplicationsNotInContingent.SingleOrDefault(s => s.FullName == studentName);
                    if (student != null)
                    {
                        return student; 
                    }
                    var splittedFullName = studentName.Split(' ', 3);
                    var (surname, name, patronimic) = (splittedFullName[0], splittedFullName[1], splittedFullName[2]);
                    student = new Student(surname, name, patronimic, curriculum.Programme.LevelOfEducation,
                        curriculum.Programme.Code, curriculum.CurriculumCode.Replace("/", @"\"), "студ");
                    
                    studentsExistedInApplicationsNotInContingent.Add(student);
                }
            }
            return student;
        }

        /// <summary>
        /// Пытается определить дисциплину из учебного плана, используя ее русское название.
        /// В случае неудачи, пропускает все столбцы с информацией о текущем заявлении
        /// </summary>
        /// <param name="disciplineRussianName">Русское название дисциплины</param>
        /// <param name="paragraphsToSkipInCaseOfFail">Количество непустых параграфов, 
        /// которые необходимо пропустить, чтобы перейти к следующему заявлению</param>
        /// <param name="discipline">Найденная дисциплина либо null, если дисциплины с таким названием нет</param>
        /// <returns>True, если дисциплина с заданным русским названием была найдена, в противном случае false</returns>
        private protected bool TryParseDiscipline(string disciplineRussianName, out Discipline discipline)
        {
            discipline = curriculum.Disciplines.SingleOrDefault(d => d.RussianName == disciplineRussianName);
            if (discipline != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Пытается распарсить основную информацию об элективном блоке по строке
        /// </summary>
        /// <param name="blockInfo">Строка и иформацией об элективном блоке</param>
        /// <param name="currentClearedCurriculumCode">Код учебного плана, заявления которого мы обрабатываем без разделителей</param>
        /// <param name="semester">Семестр</param>
        /// <param name="blockNumber">Номер блока</param>
        /// <param name="specialization">Специализация/кафедра</param>
        /// <param name="applicationsCount">Количество заявлений в данном блоке</param>
        /// <returns>True, если блок является элективным и информацию о нем можно распарсить, в противном случае false</returns>
        private protected bool TryParseInfoAboutElectiveBlock(string blockInfo, string currentClearedCurriculumCode,
            out int semester, out int blockNumber, out string specialization, out int applicationsCount)
        {
            // тут различные вариации написания
            if (clearedCurriculumCode == currentClearedCurriculumCode)
            {
                var match = Regex.Match(blockInfo, @"по выбору С(\d{2})_(\d+)\s+\(.+\){1}?\s+\(Кол-во=(\d+)\)");
                if (match.Success)
                {
                    semester = int.Parse(match.Groups[1].Value);
                    blockNumber = int.Parse(match.Groups[2].Value);
                    specialization = null;
                    applicationsCount = int.Parse(match.Groups[3].Value);
                    return true;
                }

                match = Regex.Match(blockInfo, @"Спецкурс\s+(\d+)\.(\d+)\s+по\s+профилю\s+([^(]+)\s+\(Кол-во=(\d+)\)");
                if (match.Success)
                {
                    semester = int.Parse(match.Groups[1].Value);
                    blockNumber = -1;
                    specialization = match.Groups[3].Value;
                    applicationsCount = int.Parse(match.Groups[4].Value);
                    return true;
                }

                match = Regex.Match(blockInfo, @"Спецсеминар С(\d{2})\s+\(.+\)\s+\(Кол-во=(\d+)\)");
                if (match.Success)
                {
                    semester = int.Parse(match.Groups[1].Value);
                    blockNumber = -1;
                    specialization = null;
                    applicationsCount = int.Parse(match.Groups[2].Value);
                    return true;
                }
            }

            semester = -1;
            blockNumber = -1;
            specialization = null;
            applicationsCount = -1;

            var applicationsCountMatch = Regex.Match(blockInfo, @"\(Кол-во=(\d+)\)");
            if (applicationsCountMatch.Success)
            {
                applicationsCount = int.Parse(applicationsCountMatch.Groups[1].Value);
            }

            for (var i = 0; i < applicationsCount * applicationFileColumnsCount; ++i)
            {
                GetNextLine();
            }

            return false;
        }

        /// <summary>
        /// Определить элективный блок, которому принадлежат дисциплины из заявлений
        /// </summary>
        /// <param name="specializationName">Специализация/кафедра</param>
        /// <param name="encounteredDisciplines">Дисциплины принадлжащие одному блоку, которые встретились в заявлениях</param>
        /// <returns>Элективный блок из учебого плана, которому принадлежат встретившиеся дисциплины</returns>
        private protected ElectivesBlock DetermineElectiveBlock(string specializationName, HashSet<Discipline> encounteredDisciplines)
        {
            var blocks = curriculum.ElectiveBlocks;
            var potentialBlocks = specializationName != null
                ? blocks.Where(b => b.Semester == semester && b.Specialization?.Name == specializationName)
                : blocks.Where(b => b.Semester == semester);

            var electivesBlock = potentialBlocks
                .SingleOrDefault(b => encounteredDisciplines
                    .All(d => b.Disciplines.Select(p => p.Discipline).Contains(d)));

            if (electivesBlock == null)
            {
                var stringBuilder = new StringBuilder();
                foreach (var d in encounteredDisciplines)
                {
                    stringBuilder.Append(d.RussianName);
                }
                var disciplines = stringBuilder.ToString();
                throw new ApplicationParsingException($"Не удалось установить элективный блок, содержащий дисциплины {disciplines}");
            }

            return electivesBlock;
        }
    }
}
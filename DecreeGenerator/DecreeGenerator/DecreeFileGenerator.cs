using ApplicationsParser;
using ContingentParser;
using CurriculumParser;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DecreeGenerator
{
    /// <summary>
    /// Содержит методы для генерации приложения к распоряжению о распределении на курсы по выбору
    /// Для каждого блока генерирует список дисциплин, на которые хоть кто-то был зачислен, 
    /// для каждой дисциплины генерирует список студентов, зачисленных на нее
    /// </summary>
    public static class DecreeFileGenerator
    {
        /// <summary>
        /// Генерирует приложение к распоряжению о распределении студентов на курсы по выбору с разбиением по элективным блокам
        /// </summary>
        /// <param name="fileName">Имя создаваемого файла</param>
        /// <param name="distribution">Распределение студентов на дисциплины п выбору с разбиением на элективные блоки</param>
        public static void GenerateDecree(string fileName, List<(Student, CommentType)> nonProcessedStudents,
            Dictionary<ElectivesBlock, Dictionary<Discipline, List<(Student, CommentType)>>> distribution)
        {
            using var patternDocument = WordprocessingDocument.Open("decreePatterns/AnnexPattern.docx", false);
            using var wordDocument = WordprocessingDocument.Create(fileName, WordprocessingDocumentType.Document);

            wordDocument.AddMainDocumentPart().Document = (Document)patternDocument.MainDocumentPart.Document.CloneNode(true);

            var body = wordDocument.MainDocumentPart.Document.Body;
            AppendNonProcessedStudentsList(body, nonProcessedStudents);
            AppendListHeader(body, "осеннем / весеннем", "20/20");
            AppendListContent(body, distribution);

            wordDocument.MainDocumentPart.Document.Save();
        }

        private static void AppendNonProcessedStudentsList(Body body, List<(Student Student, CommentType Comment)> nonProcessedStudents)
        {
            var paragraphPr = new ParagraphProperties(new AdjustRightIndent { Val = false },
            new AutoSpaceDE { Val = false }, new AutoSpaceDN { Val = false },
            new SpacingBetweenLines { After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto });

            if (nonProcessedStudents.Count == 0)
            {
                AppendLine(body, "Заявления всех студентов были обработаны!:)");

                AppendLine(body, "1. Убедитесь, что в контингенте содержатся актуальные данные об ушедших и вернувшихся из академического отпуска, " +
                    "отчислившихся, восстановившихся, менявших направление обучения");

                AppendLine(body, "2. Просмотрите списки студентов на наличие людей, попавших в элективный блок дважды. " +
                    "Используя заявления, самостоятельно выберите, на какую дисциплину зачислять студента.");

                AppendLine(body, "3. Посмотрите, какие студенты не подавали заявления. Вы можете определить их на другую дисциплину, если считаете это необходимым");

                AppendLine(body, "4. Теперь необходимо сравнить число людей в каждом элективном блоке с числом студентов, которое там должно быть. " +
                    "Если они равны, то, скорее всего, распоряжение правильное. В противном случае обратитесь к шагу 1.");

                return;
            }

            AppendLine(body, "Заявления некоторых студентов не были обработаны.");

            AppendLine(body, "1. Убедитесь, что в контингенте содержатся актуальные данные об ушедших и вернувшихся из академического отпуска, " +
                "отчислившихся, восстановившихся, студентах. менявших направление обучения");

            AppendLine(body, "2. Если это возможно, устраните причины, по которым заявления следующих студентов не были обработаны " +
                "и повторите попытку для получения нового распределения.");

            var nonProcessed = nonProcessedStudents.Distinct().ToList().OrderBy(s => s.Student.FullName).ToList();
            var encounteredComments = nonProcessed.Select(p => p.Comment).Distinct().ToList();
            foreach (var comment in encounteredComments)
            {
                var students = nonProcessed.Where(p => p.Comment == comment).Select(p => p.Student).Distinct().ToList();
                AppendLine(body, DefineCommentTypeDescription(comment));
                foreach (var student in students)
                {
                    AppendLine(body, student.FullName);
                }
            }

            AppendLine(body, "2. Просмотрите списки студентов на наличие людей, попавших в элективный блок дважды. " +
                "Используя заявления, самостоятельно выберите, на какую дисциплину зачислять студента.");

            AppendLine(body, "3. Посмотрите, какие студенты не подавали заявления. Вы можете определить их на другую дисциплину, если считаете это необходимым");

            AppendLine(body, "4. Теперь необходимо сравнить число людей в каждом элективном блоке с числом студентов, которое там должно быть. " +
                "Если они равны, то, скорее всего, распоряжение правильное. В противном случае обратитесь к шагу 1.");
        }

        private static string DefineCommentTypeDescription(CommentType commentType)
        {
            switch (commentType)
            {
                case CommentType.AppliedStudentIsNotListedInContingent:
                    return "Следующие студенты отсутствуют в контингенте. Для каждого студента проверьте должен ли он находиться в контингенте. " +
                        "Если должен, то добавьте его в контингент. В противном случае проигнорируйте сообщение.";
                case CommentType.Ok:
                    return "";
                case CommentType.StudentHasAlreadyAppearedInBlock:
                    return " ---- зачислен на несколько дисциплин в данном блоке";
                case CommentType.StudentAppliedForElectiveLackingInCurriculum:
                    return "Следующие студенты подавались на дисциплины, отсутствующие в обрабатываемом учебном плане. " +
                        "Для каждого студента проверьте, что учебный план, по которому он обучается, согласно контингенту, соответствует действительности. " +
                        "Если это так, то просто проигнорируйте данное сообщение для проверяемого студента.";
                case CommentType.StudentDidNotApplyForElectives:
                    return " ---- распределен произвольно";
                case CommentType.StudentEnrolledInDifferentCurriculumJudgingByTheContingent:
                    return "Согласно контингенту, код учебного плана следующих студентов отличается от обрабатываемого. " +
                        "Для каждого студента проверьте, что учебный план, по которому он обучается, согласно контингенту, соответствует действительности. " +
                        "Если это так, то просто проигнорируйте данное сообщение для проверяемого студента.";
            }
            return "";
        }

        /// <summary>
        /// Генерирует заголовок списка студентов
        /// </summary>
        /// <param name="body">Тело документа, в который ведется запись</param>
        /// <param name="semester">Семестр</param>
        /// <param name="year">Год</param>
        private static void AppendListHeader(Body body, string semester, string year)
        {
            var listHeader = $"Списки студентов на элективные дисциплины в {semester} семестре {year} учебного года:";
            AppendLine(body, listHeader);
        }

        private static void AppendLine(Body body, string line)
        {
            var pPr = new ParagraphProperties(new AdjustRightIndent() { Val = false },
                new AutoSpaceDE() { Val = false }, new AutoSpaceDN() { Val = false },
                new SpacingBetweenLines() { After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto });

            var paragraph = body.AppendChild(new Paragraph(pPr));
            paragraph.AppendChild(new Run((RunProperties)commonRunPr.CloneNode(true), new Text(line)));
            paragraph.AppendChild(new Break());
        }

        /// <summary>
        /// Генерирует список студентов
        /// </summary>
        /// <param name="body">Тело документа, в который ведется запись</param>
        /// <param name="distribution">Распределение студентов</param>
        private static void AppendListContent(Body body, Dictionary<ElectivesBlock, Dictionary<Discipline, List<(Student Student, CommentType Comment)>>> distribution)
        {
            var blockNameRunPr = (RunProperties)commonRunPr.CloneNode(true);
            blockNameRunPr.AppendChild(new Bold());

            var disciplineNameRunPr = (RunProperties)commonRunPr.CloneNode(true);
            disciplineNameRunPr.AppendChild(new Italic());
            disciplineNameRunPr.AppendChild(new NoProof());
            disciplineNameRunPr.AppendChild(new Underline() { Val = UnderlineValues.Single });

            foreach (var (electivesBlock, disciplineStudents) in distribution)
            {
                if (disciplineStudents.Count == 0)
                {
                    continue;
                }
                var blockParagraph = body.AppendChild(new Paragraph());
                var blockRun = blockParagraph.AppendChild(new Run((RunProperties)blockNameRunPr.CloneNode(true)));
                blockRun.AppendChild(new Text(GetElectiveBlockName(electivesBlock)));
                foreach (var (discipline, students) in disciplineStudents)
                {
                    if (students.Count == 0)
                    {
                        continue;
                    }

                    var disciplineParagraph = body.AppendChild(new Paragraph());
                    var disciplineRunPr = (RunProperties)disciplineNameRunPr.CloneNode(true);
                    var disciplineRun = disciplineParagraph.AppendChild(new Run(disciplineRunPr));
                    disciplineRun.AppendChild(new Text(discipline.RussianName));
                    var sortedStudents = students.OrderBy(s => s.Student.FullName);
                    foreach (var (student, comment) in sortedStudents)
                    {
                        var pPrStudent = (ParagraphProperties)studentParagraphPr.CloneNode(true);
                        var studentParagraph = body.AppendChild(new Paragraph(pPrStudent));

                        var additionalInfo = DefineCommentTypeDescription(comment);
                        var rPrCommon = (RunProperties)commonRunPr.CloneNode(true);
                        var text = new StringBuilder(student.FullName).Append(" ")
                            .Append(student.GroupInContingent)
                            .Append($"{additionalInfo}")
                            .ToString();

                        studentParagraph.AppendChild(new Run(rPrCommon, new Text(text)));
                    }
                    body.AppendChild(new Break());
                }
            }
        }

        /// <summary>
        /// Получая элективный блок, возвращает его номер или наименоание специализации
        /// </summary>
        /// <param name="electivesBlock">Элективный блок</param>
        /// <returns>Номер или наименоание специализации</returns>
        private static string GetElectiveBlockName(ElectivesBlock electivesBlock)
            => electivesBlock.Specialization == null 
            ? $"Блок {electivesBlock.Number}" 
            : electivesBlock.Specialization.Name;

        /// <summary>
        /// Общие свойства всех пробегов в файле
        /// </summary>
        private static readonly RunProperties commonRunPr = new RunProperties(
            new RunFonts() { Ascii = "Times New Roman", EastAsia = "Times New Roman", HighAnsi = "Times New Roman" },
            new RunStyle() { Val = "FakeCharacterStyle" },
            new Languages() { EastAsia = "ru-RU" },
            new FontSize() { Val = "24" },
            new FontSizeComplexScript() { Val = "24" });

        private static readonly ParagraphProperties studentParagraphPr = new ParagraphProperties(new AdjustRightIndent { Val = false },
            new AutoSpaceDE { Val = false }, new AutoSpaceDN { Val = false },
            new SpacingBetweenLines { After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto },
            new NumberingProperties { NumberingId = new NumberingId { Val = 1 } });
    }
}

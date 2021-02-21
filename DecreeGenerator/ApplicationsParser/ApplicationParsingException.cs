using System;
using System.Runtime.Serialization;

namespace ApplicationsParser
{
    /// <summary>
    /// Представляет ошибки, которые возникли во время парсинга файлов с заявлениями студентов
    /// </summary>
    [Serializable]
    internal class ApplicationParsingException : Exception
    {
        /// <summary>
        /// Инициализирует новый экземляр класса <name>ApplicationParsingException</name>
        /// </summary>
        public ApplicationParsingException()
        {
        }

        /// <summary>
        /// Инициализирует новый экземляр класса <name>ApplicationParsingException</name> 
        /// с соббщением об ошибке
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        public ApplicationParsingException(string message) 
            : base(message)
        {
        }

        /// <summary>
        /// Инициализирует новый экземляр класса <name>ApplicationParsingException</name> 
        /// с сообщением об ошибке и исключением, вызвавшим данное
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="innerException">Исключение, вызвавшее данное исключение</param>
        public ApplicationParsingException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Инициализирует новый экземляр класса <name>ApplicationParsingException</name> c сериализованными данными
        /// </summary>
        /// <param name="info">Содержит данные сериализованного объекта об исключении, которое было брошено</param>
        /// <param name="context">Содежит информацию об источнике или назначении</param>
        protected ApplicationParsingException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}
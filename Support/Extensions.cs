using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Con = System.Diagnostics.Debug;

namespace UpdateViewer.Support
{
    public static class Extensions
    {
        /// <summary>
        /// Simplifies console printing
        /// </summary>
        /// <param name="msg">text to print</param>
        /// <param name="col">color of text (optional)</param>
        public static void Print(this string msg, ConsoleColor col = ConsoleColor.Gray)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.WriteLine(msg);
        }

        /// <summary>
        /// Reverses any string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Reverse(this string str)
        {
            if (string.IsNullOrEmpty(str))
                throw new Exception("Cannot reverse an empty string.");

            char[] charArray = str.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        /// <summary>
		/// See if a process is running in memory
		/// </summary>
		/// <param name="name">The name of the process to look for.</param>
		/// <returns>True if process is found, false otherwise.</returns>
		public static bool IsProcessOpen(this string name)
        {
            try
            {
                foreach (System.Diagnostics.Process clsProcess in System.Diagnostics.Process.GetProcesses())
                {
                    if (clsProcess.ProcessName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Helper method
        /// </summary>
        /// <param name="target"></param>
        public static void OpenExplorerWindow(this string target)
        {
            try
            {
                Log.Write(eModule.MiscModel, $"> Opening folder: {target} ...", true);
                var process = new System.Diagnostics.Process();
                //To open and make folder the root: Explorer.exe /root,"\\DEVSRV03\Devshare2" 
                //To open and select a file: Explorer.exe /select,"C:\Temp\Log.txt"
                //To open and in a seperate process: Explorer.exe /seperate,"C:\Users" 
                process.StartInfo.FileName = @"explorer.exe";
                process.StartInfo.Arguments = "/seperate,\"" + target + "\"";
                process.Start();
            }
            catch (InvalidOperationException ioex)
            {
                Log.Write(eModule.MiscModel, $"OpenExplorerWindow(ERROR): {ioex.Message}", true);
            }
        }

        /// <summary>
        /// Simple XOR encryption/decryption method
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Encrypt(this string str) //call this method again to decrypt
        {
            if (string.IsNullOrEmpty(str))
                throw new Exception("Cannot encrypt an empty string.");

            byte[] bytes = Encoding.UTF8.GetBytes(str);

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ 0x5A); // Flip every other bit in each byte.
            }

            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Shuffles any type of array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static T[] Shuffle<T>(this T[] array)
        {
            Random random = new Random();
            int n = array.Length;
            while (n > 1)
            {
                n--;
                int r = random.Next(n + 1);
                T t = array[r];
                array[r] = array[n];
                array[n] = t;
            }

            return array;
        }

        public static string RemoveDiacritics(this string strThis)
        {
            if (strThis == null)
                return null;

            var sb = new StringBuilder();

            foreach (char c in strThis.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString();
        }

        //===============================================================================================================
        public static string ToFileSize(this long size)
        {
            if (size < 1024) { return (size).ToString("F0") + " Bytes"; }
            if (size < Math.Pow(1024, 2)) { return (size / 1024).ToString("F0") + "KB"; }
            if (size < Math.Pow(1024, 3)) { return (size / Math.Pow(1024, 2)).ToString("F0") + "MB"; }
            if (size < Math.Pow(1024, 4)) { return (size / Math.Pow(1024, 3)).ToString("F0") + "GB"; }
            if (size < Math.Pow(1024, 5)) { return (size / Math.Pow(1024, 4)).ToString("F0") + "TB"; }
            if (size < Math.Pow(1024, 6)) { return (size / Math.Pow(1024, 5)).ToString("F0") + "PB"; }
            return (size / Math.Pow(1024, 6)).ToString("F0") + "EB";
        }

        //===============================================================================================================
        //These may seem trivial, but in the heat of string manipulation, it's so much easier to
        //"remove the last character" from the string and get on with your life.
        public static string RemoveLastCharacter(this String instr)
        {
            return instr.Substring(0, instr.Length - 1);
        }
        public static string RemoveLast(this String instr, int number)
        {
            return instr.Substring(0, instr.Length - number);
        }
        public static string RemoveFirstCharacter(this String instr)
        {
            return instr.Substring(1);
        }
        public static string RemoveFirst(this String instr, int number)
        {
            return instr.Substring(number);
        }
        public static string GetLast(this string source, int numChars)
        {
            if (numChars >= source.Length)
                return source;

            return source.Substring(source.Length - numChars);
        }


        //===============================================================================================================
        //Check to see if a date is between two dates.
        public static bool Between(this DateTime dt, DateTime rangeBeg, DateTime rangeEnd)
        {
            return dt.Ticks >= rangeBeg.Ticks && dt.Ticks <= rangeEnd.Ticks;
        }

        //===============================================================================================================
        //Figure out how old something is.
        public static int CalculateAge(this DateTime dateTime)
        {
            int age = DateTime.Now.Year - dateTime.Year;
            if (DateTime.Now < dateTime.AddYears(age))
            {
                age--;
            }

            return age;
        }

        //===============================================================================================================
        //Based on the time, it will display a readable sentence as to when that time
        //happened(i.e. 'One second ago' or '2 months ago')
        public static string ToReadableTime(this DateTime value, bool useUTC = false)
        {
            TimeSpan ts;

            if (useUTC)
                ts = new TimeSpan(DateTime.UtcNow.Ticks - value.Ticks);
            else
                ts = new TimeSpan(DateTime.Now.Ticks - value.Ticks);

            double delta = ts.TotalSeconds;
            if (delta < 60)
            {
                return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";
            }
            if (delta < 120)
            {
                return "a minute ago";
            }
            if (delta < 2700) // 45 * 60
            {
                return ts.Minutes + " minutes ago";
            }
            if (delta < 5400) // 90 * 60
            {
                return "an hour ago";
            }
            if (delta < 86400) // 24 * 60 * 60
            {
                return ts.Hours + " hours ago";
            }
            if (delta < 172800) // 48 * 60 * 60
            {
                return "yesterday";
            }
            if (delta < 2592000) // 30 * 24 * 60 * 60
            {
                return ts.Days + " days ago";
            }
            if (delta < 31104000) // 12 * 30 * 24 * 60 * 60
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
            return years <= 1 ? "one year ago" : years + " years ago";
        }

        //===============================================================================================================
        //Determine if the date is a working day, weekend, or determine the next workday coming up.
        public static bool WorkingDay(this DateTime date)
        {
            return date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
        }
        public static bool IsWeekend(this DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        }
        public static DateTime NextWorkday(this DateTime date)
        {
            DateTime nextDay = date.AddDays(1);
            while (!nextDay.WorkingDay())
            {
                nextDay = nextDay.AddDays(1);
            }
            return nextDay;
        }

        //===============================================================================================================
        //Determine the Next date by passing in a DayOfWeek
        //(i.e.From this date, when is the next Tuesday?)
        public static DateTime Next(this DateTime current, DayOfWeek dayOfWeek)
        {
            int offsetDays = dayOfWeek - current.DayOfWeek;
            if (offsetDays <= 0)
            {
                offsetDays += 7;
            }
            DateTime result = current.AddDays(offsetDays);
            return result;
        }

        /// <summary>
        /// Parse XML string and return all node names & values
        /// </summary>
        /// <param name="XMLdata"></param>
        /// <returns></returns>
        private static List<string> ExtractXML(this string XMLdata)
        {
            List<string> innerText = new List<string>();
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(XMLdata);
                XmlNodeList nodes = xmlDoc.SelectNodes("*");
                foreach (XmlNode node in nodes)
                {
                    innerText.Add($"Name:{node.Name} --> InnerText:{node.InnerText}");
                }
                return innerText;
            }
            catch (Exception ex)
            {
                innerText.Add(ex.Message);
                return innerText;
            }
        }

        //===============================================================================================================
        //If you want to take a large string and convert it to a Stream or vice-versa,
        //here are three extension methods that make this the easiest way to manipulate streams.
        public static Stream ToStream(this string str)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(str);
            //byte[] byteArray = Encoding.ASCII.GetBytes(str);
            return new MemoryStream(byteArray);
        }
        public static string ToString(this Stream stream)
        {
            StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        /// <summary>
        /// Copy from one stream to another.
        /// Example:
        /// using(var stream = response.GetResponseStream())
        /// using(var ms = new MemoryStream())
        /// {
        ///     stream.CopyTo(ms);
        ///      // Do something with copied data
        /// }
        /// </summary>
        /// <param name="fromStream">From stream.</param>
        /// <param name="toStream">To stream.</param>
        public static void CopyTo(this Stream fromStream, Stream toStream)
        {
            if (fromStream == null)
            {
                throw new ArgumentNullException("fromStream is null");
            }

            if (toStream == null)
            {
                throw new ArgumentNullException("toStream is null");
            }

            byte[] bytes = new byte[8092];
            int dataRead;
            while ((dataRead = fromStream.Read(bytes, 0, bytes.Length)) > 0)
            {
                toStream.Write(bytes, 0, dataRead);
            }
        }


        /// <summary>
        /// Helper method
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static int ConvertSecondsToMilliseconds(this int seconds)
        {
            return (int)TimeSpan.FromSeconds(seconds).TotalMilliseconds;
        }

        /// <summary>
        /// Helper method
        /// </summary>
        /// <param name="minutes"></param>
        /// <returns></returns>
        public static int ConvertMinutesToMilliseconds(this int minutes)
        {
            return (int)TimeSpan.FromMinutes(minutes).TotalMilliseconds;
        }

        /// <summary>
        /// Helper method
        /// </summary>
        /// <param name="hours"></param>
        /// <returns></returns>
        public static int ConvertHoursToMilliseconds(this int hours)
        {
            return (int)TimeSpan.FromHours(hours).TotalMilliseconds;
        }

        //===============================================================================================================
        //This is a great extension when an enumerated type are flags instead of full items.
        //One example is to have an integer in a table that converts to Enumerated Flag types.
        //This is where these extensions come in handy.
        public static bool Has<T>(this System.Enum type, T value)
        {
            try
            {
                return (((int)(object)type & (int)(object)value) == (int)(object)value);
            }
            catch
            {
                return false;
            }
        }
        public static bool Is<T>(this System.Enum type, T value)
        {
            try
            {
                return (int)(object)type == (int)(object)value;
            }
            catch
            {
                return false;
            }
        }
        public static T Add<T>(this System.Enum type, T value)
        {
            try
            {
                return (T)(object)(((int)(object)type | (int)(object)value));
            }
            catch (Exception ex)
            {
                throw new ArgumentException(string.Format("Could not append value from enumerated type '{0}'.", typeof(T).Name), ex);
            }
        }
        public static T Remove<T>(this System.Enum type, T value)
        {
            try
            {
                return (T)(object)(((int)(object)type & ~(int)(object)value));
            }
            catch (Exception ex)
            {
                throw new ArgumentException(string.Format("Could not remove value from enumerated type '{0}'.", typeof(T).Name), ex);
            }
        }

        //I can't tell you how many times I've needed to convert an XmlDocument into an XDocument and vice-versa to use LINQ.
        //These handy extension methods will save you a load of time.
        public static XmlDocument ToXmlDocument(this XDocument xDocument)
        {
            XmlDocument xmlDocument = new XmlDocument();
            using (XmlReader xmlReader = xDocument.CreateReader())
            {
                xmlDocument.Load(xmlReader);
            }
            return xmlDocument;
        }
        public static XDocument ToXDocument(this XmlDocument xmlDocument)
        {
            using (XmlNodeReader nodeReader = new XmlNodeReader(xmlDocument))
            {
                nodeReader.MoveToContent();
                return XDocument.Load(nodeReader);
            }
        }
        public static XmlDocument ToXmlDocument(this XElement xElement)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings xws = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = false };
            using (XmlWriter xw = XmlWriter.Create(sb, xws))
            {
                xElement.WriteTo(xw);
            }
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(sb.ToString());
            return doc;
        }
        public static Stream ToMemoryStream(this XmlDocument doc)
        {
            MemoryStream xmlStream = new MemoryStream();
            doc.Save(xmlStream);
            xmlStream.Flush(); // Adjust this if you want to read your data 
            xmlStream.Position = 0;
            return xmlStream;
        }


        //===============================================================================================================
        /// <summary>
        /// Generate random text output
        /// </summary>
        /// <param name="minWords"></param>
        /// <param name="maxWords"></param>
        /// <param name="minSentences"></param>
        /// <param name="maxSentences"></param>
        /// <param name="numParagraphs"></param>
        /// <returns>Built data string</returns>
        public static string LoremIpsumGenerator(int minWords, int maxWords, int minSentences, int maxSentences, int numParagraphs)
        {

            string[] words = new[]{  "lorem", "ipsum", "dolor", "sit", "amet", "consectetuer",
                                     "adipiscing", "elit", "sed", "do", "eiusmod", "tempor", 
                                     "incididunt", "ut", "labore", "et", "dolore", "magna", 
                                     "aliqua", "enim", "ad", "minim", "veniam", "quis", 
                                     "nostrud", "exercitation", "ullamco", "laboris", "nisi", 
                                     "aliquip", "commodo", "consequat", "duis", "aute", "irure",
                                     "reprehenderit", "voluptate", "velit", "fugiat", "pariatur"};

            /*
            Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt 
            ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco 
            laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in 
            voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat 
            non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
             */

            Random rand = new Random();
            int numSentences = rand.Next(maxSentences - minSentences) + minSentences + 1;
            int numWords = rand.Next(maxWords - minWords) + minWords + 1;

            StringBuilder result = new StringBuilder();

            for (int p = 0; p < numParagraphs; p++)
            {
                result.Append(Environment.NewLine);
                for (int s = 0; s < numSentences; s++)
                {
                    for (int w = 0; w < numWords; w++)
                    {
                        if (w > 0)
                        {
                            result.Append(" ");
                        }
                        result.Append(words[rand.Next(words.Length)]);
                    }
                    result.Append(". ");
                }
                result.Append(Environment.NewLine);
            }

            return result.ToString();
        }

        /// <summary>
        /// Helper method
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int GetWordCount(this string str)
        {
            if (!String.IsNullOrEmpty(str))
                return str.Split(' ').Length;
            return 0;
        }

        /// <summary>
        /// This function uses the substring routine to take the first <paramref name="length"/> characters in 
        /// a string.  If the string is less that <paramref name="length"/>, it will return the entire string.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="length">Length to take of the string</param>
        /// <returns></returns>
        public static string Take(this string s, int length)
        {
            if (s.Length > length)
                return s.Substring(0, length).Trim();

            return s;
        }

        /// <summary>
        /// This function uses the substring routine to take the first <paramref name="length"/> characaters in 
        /// a string.  If the string is larger than <paramref name="length"/>, the <paramref name="message"/> text will be
        /// appended to the end of the string instead.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="length"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string Truncate(this string s, int length, string message)
        {
            if (s.Length > length)
                return String.Format("{0}... {1}", s.Substring(0, length).Trim(), message);

            return s;
        }


        /// <summary>
        /// This function converts an object to a boolean value, the object can be nullable.  Default value for nullable is false.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool ToBoolean(this object s)
        {
            if (s is DBNull || s == null)
                return false;
            bool result;
            bool.TryParse(s.ToString(), out result);
            return result;
        }
        /// <summary>
        /// This function converts an object to a boolean value, the object can be nullable.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="nullable"></param>
        /// <returns></returns>
        public static System.Nullable<bool> ToBoolean(this object o, bool nullable)
        {
            if (((o is DBNull) || (o == null)) && (nullable))
                return null;
            else
                return o.ToBoolean();
        }

        /// <summary>
        /// Returns the fractional portion of a decimal number.
        /// </summary>
        /// <param name="number">The number to determine the fraction of.</param>
        /// <param name="includeTrailingZeros">If true, trailing significant zeros are included, otherwise they are stripped off.</param>
        /// <returns>The fractional portion of the decimal or an empty string if there is no fractional portion.</returns>
        public static string GetFractional(this decimal number, bool includeTrailingZeros = false)
        {
            string[] partsOfDecimal = number.ToString(CultureInfo.InvariantCulture).Split('.');

            if (partsOfDecimal.Length < 2)
                return "";

            if (!includeTrailingZeros)
                partsOfDecimal[1] = partsOfDecimal[1].TrimEnd('0');
            return partsOfDecimal[1];
        }

        /// <summary>
        /// Determines if the specified string is null, empty, or all whitespace.
        /// </summary>
        /// <param name="str">The string to check.</param>
        /// <returns>True if the string is null, empty, or all whitespace. False otherwise.</returns>
        public static bool IsNullEmptyOrBlankString(this string str)
        {
            if (str == null)
                return true;

            return str.Trim() == string.Empty;
        }

        /// <summary>
        /// Converts a DateTime to a DateTimeOffset with the specified offset
        /// </summary>
        /// <param name="date">The DateTime to convert</param>
        /// <param name="offset">The offset to apply to the datetime field</param>
        /// <returns>The corresponding DateTimeOffset</returns>
        public static DateTimeOffset ToOffset(this DateTime date, TimeSpan offset)
        {
            return new DateTimeOffset(date).ToOffset(offset);
        }

        /// <summary>
        /// Converts an object into a JSON string
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string ToJson(this object item)
        {
            var ser = new DataContractJsonSerializer(item.GetType());
            using (var ms = new MemoryStream())
            {
                ser.WriteObject(ms, item);
                var sb = new StringBuilder();
                sb.Append(Encoding.UTF8.GetString(ms.ToArray()));
                return sb.ToString();
            }
        }
        public static string ToJson(this IEnumerable collection, string rootName)
        {
            var ser = new DataContractJsonSerializer(collection.GetType());
            using (var ms = new MemoryStream())
            {
                ser.WriteObject(ms, collection);
                var sb = new StringBuilder();
                sb.Append("{ \"").Append(rootName).Append("\": ");
                sb.Append(Encoding.UTF8.GetString(ms.ToArray()));
                sb.Append(" }");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Converts a JSON string into the specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        public static T FromJsonTo<T>(this string jsonString)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
            {
                T jsonObject = (T)ser.ReadObject(ms);
                return jsonObject;
            }
        }

        /// <summary>
        /// Tests whether an array contains the index, and returns the value if true or the defaultValue if false
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string GetIndex(this string[] array, int index, string defaultValue = "") => (index < array.Length) ? array[index] : defaultValue;

        /// <summary>
        /// Tests whether an array contains the index, and returns the value if true or the defaultValue if false
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int GetIndex(this int[] array, int index, int defaultValue = -1) => (index < array.Length) ? array[index] : defaultValue;

        /// <summary>
        /// Tests whether an array contains the index, and returns the value if true or the defaultValue if false
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetIndex<T>(this T[] array, int index, T defaultValue) => (index < array.Length) ? array[index] : defaultValue;

        /// <summary>
        /// Tests whether a guid is null or equal to the empty guid
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>True if the guid is null or empty</returns>
        public static bool IsNullOrEmpty(this Guid? guid) => guid == null || guid == Guid.Empty;


        /// <summary>
        /// Chunks a large list into smaller n-sized list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="locations">List to be chunked</param>
        /// <param name="nSize">Size of chunks</param>
        /// <returns>IEnumerable list of <typeparam name="T"></typeparam> broken up into chunks</returns>
        public static IEnumerable<List<T>> SplitList<T>(List<T> locations, int nSize = 30)
        {
            for (int i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
            }
        }

        /// <summary>
        /// Masks an input string with the provided <paramref name="maskCharacter"/>, leaving <paramref name="unmaskedCharacters"/> of the original string
        /// </summary>
        /// <param name="item"></param>
        /// <param name="maskCharacter"></param>
        /// <param name="unmaskedCharacters"></param>
        /// <returns></returns>
        public static string Mask(this string item, char maskCharacter, int unmaskedCharacters)
        {
            if (string.IsNullOrWhiteSpace(item)) throw new ArgumentNullException();
            return item.Substring(item.Length - unmaskedCharacters).PadLeft(item.Length, maskCharacter);
        }

        /// <summary>
        /// Returns whether the bit at the specified position is set.
        /// </summary>
        /// <typeparam name="T">Any integer type.</typeparam>
        /// <param name="t">The value to check.</param>
        /// <param name="pos">The position of the bit to check, 0 refers to the least significant bit.</param>
        /// <returns>true if the specified bit is on, otherwise false.</returns>
        public static bool IsBitSet<T>(this T t, int pos) where T : struct, IConvertible
        {
            var value = t.ToInt64(CultureInfo.CurrentCulture);
            return (value & (1 << pos)) != 0;
        }


        /// <summary>
        /// Helper method
        /// </summary>
        /// <param name="filePath"></param>
        public static void SimpleFileEncryption(string filePath) 
        {
            /*** EXAMPLE USE ***/
            //Console.WriteLine("You can pass the encrypted file name to call the decrypt process.");
            //Console.Write("Enter the local file name to encrypt and press <Enter>: ");
            //string fileName = Console.ReadLine();
            //SimpleFileEncryption(fileName);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"ERROR: {filePath} was not found.");
                return;
            }

            byte[] bytes = File.ReadAllBytes(filePath);
            // Flip every other bit in each byte.
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ 0x55);
            }
            // Write it to a different file
            if (Path.GetExtension(filePath).Equals(".enc"))
            {
                File.WriteAllBytes(Path.ChangeExtension(filePath, ".dec"), bytes);
            }
            else
            {
                File.WriteAllBytes(Path.ChangeExtension(filePath, ".enc"), bytes);
            }
        }

    }

}

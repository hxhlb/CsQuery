﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Security.Cryptography;

namespace CsQuery.Mvc.ClientScript
{
    class ScriptParser: IDisposable
    {

        #region constructor

        public  ScriptParser(string fileName)
        {
            FileName = fileName;

        }

        #endregion

        #region private properties


        private TextReader StreamReader;
        private string _FileName;
        /// <summary>
        /// When true the parser is currently in a multilinecomment
        /// </summary>

        private bool InMultilineComment;

        /// <summary>
        /// Gets or sets a value indicating whether the last read line is a comment.
        /// </summary>
        ///
        /// <value>
        /// false if there are embedded comments inside code in a line, or no comment, and true only when
        /// the entire line is a commment.
        /// </value>

        public bool InComment;
        
        #endregion

        #region public properties

        /// <summary>
        /// When true, this script represents a physical file in the filesystem. If false, it is either
        /// invalid, or resolved in another way.
        /// </summary>

        public bool IsPhysicalFile { get; protected set; }

        /// <summary>
        /// The full text of the file
        /// </summary>

        public string FileData {get; protected set;}

        /// <summary>
        /// SHA1 has for the file data
        /// </summary>

        public string FileHash
        {
            get
            {
                return GetMD5Hash(FileData);
            }
        }
        public string FileName { 
            get {
                return _FileName;
            }
            protected set {
                string fileName = GetJustFilePath(value);

                IsPhysicalFile = IsValidFileName(fileName) 
                    && File.Exists(fileName);

                _FileName = fileName;
                if (IsPhysicalFile)
                {
                    FileData = File.ReadAllText(fileName);
                    StreamReader = new StringReader(FileData);
                }
            }
        }

        /// <summary>
        /// When true, indicates that non-whitespace, non-code lines have been parsed already.
        /// </summary>
        ///
        /// <value>
        /// true if code has been found yet.
        /// </value>

        public bool AnyCodeYet { get; protected set; }

        #endregion

        #region public methods

        /// <summary>
        /// Reads the next line from the file, stripping out comment markers (instead, setting the "InComment" property)
        /// and setting properties based on the contents of the line.
        /// </summary>
        ///
        /// <returns>
        /// The line.
        /// </returns>

        public string ReadLine()
        {
            string line= StreamReader.ReadLine();
            if (line != null)
            {
                if (InMultilineComment)
                {
                    
                    Match endComment = Patterns.EndComment.Match(line);

                    if (endComment.Success)
                    {
                        InMultilineComment = false;
                        InComment = false;
                        line = endComment.Groups["comment"].Value;
                    }
                    
                }
                else
                {
                    bool isFullLineComment = false;
                    Match singleLineComment = Patterns.FullLineComment.Match(line);

                    bool isSingleLineComment = singleLineComment.Success;
                    Match fullLineComment = null;

                    bool isStartComment = false;
                    Match startComment = null;

                    

                    if (!InMultilineComment) {
                        if (!isSingleLineComment)
                        {
                            fullLineComment = Patterns.FullLineComment.Match(line);
                            isFullLineComment = fullLineComment.Success;
                        }

                        if (!isSingleLineComment && !isFullLineComment)
                        {
                            startComment = Patterns.StartComment.Match(line);
                            isStartComment = startComment.Success;
                        }
                    } 

                    // set comment status flags
                    if (!isFullLineComment && isStartComment)
                    {
                        InMultilineComment = true;
                        InComment = true;
                        line = startComment.Groups["comment"].Value;
                    }
                    else if (isFullLineComment)
                    {
                        InComment = true;
                        line = fullLineComment.Groups["comment"].Value;
                    } else if (isSingleLineComment) {
                        InComment = true;
                        line = singleLineComment.Groups["comment"].Value;
                    }
                    else if (!AnyCodeYet && !Patterns.WhiteSpace.IsMatch(line))
                    {
                        AnyCodeYet = true;
                    }
                }
            }
            return line;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Test if the path appears to be a valid file name
        /// </summary>
        ///
        /// <param name="path">
        /// Full pathname of the file.
        /// </param>
        ///
        /// <returns>
        /// true if valid file name, false if not.
        /// </returns>

        private bool IsValidFileName(string path)
        {
            int lastDotIndex = path.LastIndexOf(".");
            int lastSlashIndex = path.LastIndexOf("\\");

            return (lastDotIndex == 0
                || lastDotIndex > lastSlashIndex);
        }
        /// <summary>
        /// Gets just file path, removing querystring
        /// </summary>
        ///
        /// <param name="path">
        /// Full pathname of the file.
        /// </param>
        ///
        /// <returns>
        /// The just file path.
        /// </returns>

        private string GetJustFilePath(string path)
        {
            int qPos = path.IndexOf("?");
            return  qPos < 0 ?
                path :
                path.Substring(0, qPos);
        }

        private string GetMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString();
        }

        void IDisposable.Dispose()
        {
            StreamReader.Dispose();
        }

        #endregion
    }
}

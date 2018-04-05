﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orbis.Events
{
    class Logger
    {
        private const string DEFAULT_TYPE = "normal";   // Default log type
        private List<Log> log;                          // List that contains all the logs
        private static Logger logger;

        private Logger()
        {
            log = new List<Log>();
        }

        /// <summary>
        /// Get instance of logger
        /// </summary>
        /// <returns></returns>
        public static Logger GetInstance()
        {
            if (logger == null)
            {
                logger = new Logger();
            }
            return logger;
        }

        /// <summary>
        /// Add a new log
        /// </summary>
        /// <param name="item">The text to log</param>
        /// <param name="type">The type</param>
        public void AddLog(string item, string type = DEFAULT_TYPE)
        {
            log.Add(new Log(item, type));
        }

        /// <summary>
        /// Fetch the current log
        /// </summary>
        /// <returns>The log</returns>
        public List<Log> GetLog()
        {
            return log;
        }

        /// <summary>
        /// Get a log by type
        /// </summary>
        /// <param name="type">The type to fetch</param>
        /// <returns>List of logs</returns>
        public List<Log> GetLogByType(string type)
        {
            // Create a local list with logs
            List<Log> tempLog = new List<Log>();

            foreach (var logItem in log)
            {
                // If the type matches the given type, put in new list
                if (logItem.Type == type)
                {
                    tempLog.Add(logItem);
                }
            }

            // Return local list
            return tempLog;
        }
    }
}

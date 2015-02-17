﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicBeePlugin
{
    class Logger
    {
        private List<Tuple<DateTime, String>> _log;

        public EventHandler OnLogUpdated;

        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Logger();
                return _instance;
            }
        }

        private static Logger _instance;
        
        private Logger()
        {
            _log = new List<Tuple<DateTime, String>>();

        }

        

        public void Log(string text)
        {
            _log.Add(new Tuple<DateTime, String>(DateTime.Now, text));
            if (OnLogUpdated != null)
                OnLogUpdated(this, new EventArgs());
        }

        public List<Tuple<DateTime, String>> LogData { get { return _log; } }

        public string LastLog
        {
            get
            {
                if (_log.Count > 0)
                {
                    return _log[_log.Count - 1].Item2;
                }
                else
                {
                    return "";
                }
            }
        }

    }
}

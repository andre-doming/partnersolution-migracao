using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SolutionsTools.DAL
{
    public class Response
    {
        string message;
        bool status;
        object _return;

        public string Message
        {
            get
            {
                return message;
            }

            set
            {
                message = value;
            }
        }

        public object Return
        {
            get
            {
                return _return;
            }

            set
            {
                _return = value;
            }
        }

        public bool Status
        {
            get
            {
                return status;
            }

            set
            {
                status = value;
            }
        }
    }
}
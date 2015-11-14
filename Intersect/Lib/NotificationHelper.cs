using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Intersect.Lib
{
    class NotificationHelper
    {
        public delegate void NotificationEvent();
        private Dictionary<string, List<NotificationEvent>> eventDict;

        private NotificationHelper()
        {
            eventDict = new Dictionary<string, List<NotificationEvent>>();
        }

        private void register(string eventName, NotificationHelper.NotificationEvent eventHandler)
        {
            if (eventDict.ContainsKey(eventName))
            {
                List<NotificationEvent> eventList = eventDict[eventName];
                eventList.Add(eventHandler);
            }
            else
            {
                eventDict.Add(eventName, new List<NotificationEvent>() { eventHandler});
            }
        }

        private void trigger(string eventName)
        {
            if (eventDict.ContainsKey(eventName))
            {
                List<NotificationEvent> eventList = eventDict[eventName];
                foreach (NotificationEvent evt in eventList)
                {
                    evt();
                }
            }
        }

        //单例模式
        private static NotificationHelper notifyer = null;

        private static NotificationHelper getInstance()
        {
            if (NotificationHelper.notifyer == null)
            {
                NotificationHelper.notifyer = new NotificationHelper();
                return NotificationHelper.notifyer;
            }
            else
            {
                return notifyer;
            }
        }

        public static void Register(string eventName, NotificationHelper.NotificationEvent eventHandler)
        {
            NotificationHelper.getInstance().register(eventName, eventHandler);
        }

        public static void Trigger(string eventName)
        {
            NotificationHelper.getInstance().trigger(eventName);
        }
    }
}

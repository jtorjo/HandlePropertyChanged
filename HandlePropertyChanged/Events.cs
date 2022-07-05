using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App2.events
{
    public interface IEventUnsubscribe {
        public void Unsubscribe();
    }


    /*
     *FIXME -> handle reflection
     *
     * if I subscribe to class T, look at all its properties.
     * if another property has INotifyPropertyChanged -> subscribe to that as well.
     * LATER if a property is an observablecollection -> subscribe to its elements
     * LATER if a property is a readonlylist -> subscribe to its elements
     *
     * - FIXME what do i do when i get a propertychanged on an aggregate object?
     *
     * path => allow getting the right object. so i create my own handler that will ping back the original object + extra path
     */

    public static class Events {
        private static object Lock = new object();
        private static Dictionary<string, Dictionary<string, IEventUnsubscribe> > events_ = new ();

        private class UnsubscribeAction : IEventUnsubscribe {
            private INotifyPropertyChanged publisher_;
            private PropertyChangedEventHandler handler_;

            public UnsubscribeAction(INotifyPropertyChanged publisher, PropertyChangedEventHandler handler) {
                this.handler_ = handler;
                this.publisher_ = publisher;
            }

            public void Unsubscribe() {
                publisher_.PropertyChanged -= handler_;
            }
        }

        private class UnsubscribeCollection<T> : IEventUnsubscribe where T : INotifyPropertyChanged {
            private ObservableCollection<T> coll_;
            private NotifyCollectionChangedEventHandler collHandler_;
            private PropertyChangedEventHandler handler_;

            public UnsubscribeCollection(ObservableCollection<T> coll, NotifyCollectionChangedEventHandler coll_handler, PropertyChangedEventHandler handler) {
                coll_ = coll;
                collHandler_ = coll_handler;
                handler_ = handler;
            }

            public void Unsubscribe() {
                foreach (var item in coll_)
                    item.PropertyChanged -= handler_;
                coll_.CollectionChanged -= collHandler_;
            }
        }

        private class UnsubscribeEnumerable<T> : IEventUnsubscribe where T : INotifyPropertyChanged {
            private List<T> list_ = new List<T>();
            private PropertyChangedEventHandler handler_;

            public UnsubscribeEnumerable(IEnumerable<T> list, PropertyChangedEventHandler handler) {
                list_ = list.ToList();
                handler_ = handler;
                foreach (var item in list)
                    item.PropertyChanged += handler_;
            }

            public void Unsubscribe() {
                foreach (var item in list_)
                    item.PropertyChanged -= handler_;
            }
        }


        private static string NewGuid() => Guid.NewGuid().ToString();




        // returns an ID you can use to unsubscribe ONLY from this
        public static string Subscribe<T>(string subscriberId, T publisher, Action<T, string> propertyChanged) where T : class,INotifyPropertyChanged {
            PropertyChangedEventHandler handler = (s, a) => propertyChanged(s as T, a.PropertyName);
            publisher.PropertyChanged += handler;

            var id = NewGuid();
            lock (Lock) {
                events_.TryAdd(subscriberId, new Dictionary<string, IEventUnsubscribe>());
                events_[subscriberId].Add(id, new UnsubscribeAction(publisher, handler));
            }

            return id;
        }

        // Subscribe to collection
        public static string Subscribe<T>(string subscriberId, ObservableCollection<T> coll, Action<T, string> propertyChanged = null, Action<T> onAdd = null, Action<T> onDel = null) where T : class, INotifyPropertyChanged {
            PropertyChangedEventHandler handler = (s, a) => propertyChanged?.Invoke(s as T, a.PropertyName);

            NotifyCollectionChangedEventHandler collHandler = (s, a) => {
                if (a.NewItems != null)
                    foreach (var item in a.NewItems.OfType<T>()) {
                        item.PropertyChanged += handler;
                        onAdd?.Invoke(item);
                    }

                if (a.OldItems != null)
                    foreach (var item in a.OldItems.OfType<T>()) {
                        item.PropertyChanged -= handler;
                        onDel?.Invoke(item);
                    }
            };
            coll.CollectionChanged += collHandler;
            foreach (var item in coll)
                item.PropertyChanged += handler;

            var id = NewGuid();
            lock (Lock) {
                events_.TryAdd(subscriberId, new Dictionary<string, IEventUnsubscribe>());
                events_[subscriberId].Add(id, new UnsubscribeCollection<T>(coll, collHandler, handler));
            }

            return id;
        }

        public static string Subscribe<T>(string subscriberId, IEnumerable<T> coll, Action<T, string> propertyChanged ) where T : class, INotifyPropertyChanged {
            PropertyChangedEventHandler handler = (s, a) => propertyChanged?.Invoke(s as T, a.PropertyName);
            var id = NewGuid();
            var unsubscribe = new UnsubscribeEnumerable<T>(coll, handler);
            lock (Lock) {
                events_.TryAdd(subscriberId, new Dictionary<string, IEventUnsubscribe>());
                events_[subscriberId].Add(id, unsubscribe);
            }

            return id;
        }




        // by unique id
        public static string Subscribe<T>(IUniqueID subscriber, T publisher, Action<T, string> propertyChanged) where T : class, INotifyPropertyChanged {
            return Subscribe(subscriber.UniqueID, publisher, propertyChanged);
        }

        public static string Subscribe<T>(IUniqueID subscbriber, ObservableCollection<T> coll, Action<T, string> propertyChanged = null, Action<T> onAdd = null, Action<T> onDel = null) where T : class, INotifyPropertyChanged {
            return Subscribe(subscbriber.UniqueID, coll, propertyChanged, onAdd, onDel);
        }

        public static string Subscribe<T>(IUniqueID subscriber, IEnumerable<T> coll, Action<T, string> propertyChanged) where T : class, INotifyPropertyChanged {
            return Subscribe(subscriber.UniqueID, coll, propertyChanged);
        }

        public static void Unsubscribe(string subscriberId) {
            Dictionary<string, IEventUnsubscribe> unsubscribe = null;
            lock (Lock) {
                events_.TryGetValue(subscriberId, out unsubscribe);
                events_.Remove(subscriberId);
            }

            if (unsubscribe != null)
                foreach (var u in unsubscribe.Values)
                    u.Unsubscribe();
        }

        public static void Unsubscribe(string subscriberId, string id) {
            IEventUnsubscribe unsubscribe = null;
            lock (Lock) 
                if (events_.TryGetValue(subscriberId, out var dictionary)) {
                    dictionary.TryGetValue(id, out unsubscribe);
                    dictionary.Remove(id);
                }
            
            unsubscribe?.Unsubscribe();
        }
    }

}

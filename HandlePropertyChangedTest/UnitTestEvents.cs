using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using App2.events;

namespace EventsSPTest
{
    public class User : INotifyPropertyChanged {
        private string first_name_ = "";
        private string last_name_ = "";

        public string FirstName {
            get => first_name_;
            set {
                if (value == first_name_) return;
                first_name_ = value;
                OnPropertyChanged();
            }
        }

        public string LastName {
            get => last_name_;
            set {
                if (value == last_name_) return;
                last_name_ = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class Tests {

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestSimpleCollection()
        {
            var coll = new ObservableCollection<User>();
            int changeCount = 0, addCount = 0, delCount = 0;
            Events.Subscribe("coll", coll, (u,_) => changeCount++, (u) => addCount++, (u) => delCount++);
            var john = new User();
            coll.Add(john);
            Assert.IsTrue(changeCount == 0 && addCount == 1 && delCount == 0);
            john.FirstName = "john";
            john.LastName = "jay";
            coll.Remove(john);
            Assert.IsTrue(changeCount == 2 && addCount == 1 && delCount == 1);
            // here, I should not be subscribed anymore
            john.FirstName = "john2";
            john.LastName = "jay2";
            Assert.IsTrue(changeCount == 2 && addCount == 1 && delCount == 1);

            var titi = new User();
            coll.Add(titi);
            Assert.IsTrue(changeCount == 2 && addCount == 2 && delCount == 1);
            titi.FirstName = "titi";
            titi.LastName = "titi";
            Assert.IsTrue(changeCount == 4 && addCount == 2 && delCount == 1);
            coll.Remove(titi);
            titi.FirstName = "titi2";
            titi.LastName = "titi2";
            Assert.IsTrue(changeCount == 4 && addCount == 2 && delCount == 2);
            Events.Unsubscribe("coll");
            // we're unsubscribed, nothing should happen
            coll.Add(john);
            coll.Add(titi);
            john.FirstName = "john3";
            titi.FirstName = "titi3";
            Assert.IsTrue(changeCount == 4 && addCount == 2 && delCount == 2);
        }

        [Test]
        public void TestSimpleList() {
            User john = new User(), titi = new User();
            List<User> users = new() {
                john, titi
            };
            int changeCount = 0;
            Events.Subscribe("list", users, (u,_) => changeCount++);
            john.FirstName = "John";
            john.LastName = "Jay";
            titi.FirstName = "titi";
            Assert.IsTrue(changeCount == 3);
            
            // this should not affect anything, since we subscribed to the elements themselves
            users.Clear();
            
            john.FirstName = "John 2";
            john.LastName = "Jay 2";
            titi.FirstName = "titi 2";
            Assert.IsTrue(changeCount == 6);

            Events.Unsubscribe("list");
            john.FirstName = "John 3";
            john.LastName = "Jay 3";
            titi.FirstName = "titi 3";
            Assert.IsTrue(changeCount == 6);
        }

        [Test]
        public void TestSimpleUser() {
            User john = new User(), titi = new User();
            int changeCount = 0;
            Events.Subscribe("john", john, (u, _) => changeCount++);
            Events.Subscribe("titi", titi, (u, _) => changeCount++);

            john.FirstName = "john";
            john.LastName = "jay";
            titi.FirstName = "titi";
            titi.LastName = "titi";
            Assert.IsTrue(changeCount == 4);

            Events.Unsubscribe("john");
            john.FirstName = "john 2";
            john.LastName = "jay 2";
            titi.FirstName = "titi 2";
            titi.LastName = "titi 2";
            Assert.IsTrue(changeCount == 6);

            Events.Unsubscribe("titi");
            john.FirstName = "john 3";
            john.LastName = "jay 3";
            titi.FirstName = "titi 3";
            titi.LastName = "titi 3";
            Assert.IsTrue(changeCount == 6);
        }
    }
}
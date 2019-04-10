using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VideoKlub
{
    /// <summary>
    /// Interaction logic for AddUserWindow.xaml
    /// </summary>
    public partial class AddUserWindow : Window
    {
        public AddUserWindow()
        {
            InitializeComponent();
        }

        private void AddUserCancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddUserOKButton_Click(object sender, RoutedEventArgs e)
        {
            AddUserParameters newUser = new AddUserParameters
            {
                FirstName = firstNameTextbox.Text,
                LastName = lastNameTextbox.Text,
                StreetAddress = addressTextbox.Text,
                City = cityTextbox.Text
            };
            if (postalCodeTextbox.Text.Length > 10)
            {
                MessageBox.Show("Postal code too long!");
                return;
            }
            newUser.PostalCode = postalCodeTextbox.Text;
            if (AllFieldsFilled(newUser))
            {
                AddUserToBase(newUser);
                MessageBox.Show(String.Format("Korisnik {0} {1} added to the database.", newUser.FirstName, newUser.LastName));
                this.Close();
            }
        }

        private bool AllFieldsFilled(AddUserParameters newUserData)
        {
            //Check if all info for the user has been entered
            PropertyInfo[] properties = typeof(AddUserParameters).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (String.IsNullOrWhiteSpace((property.GetValue(newUserData).ToString())))
                    {
                        MessageBox.Show("All fields are required!");
                        return false;
                    }
            }
            return true;
        }

        private void AddUserToBase(AddUserParameters newUser)
        {
            //Add user to the database after the sanity check
            string connectionString = ConfigurationManager.ConnectionStrings["VideoKlub.Properties.Settings.VideoKlubMarkoConnectionString"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = "INSERT INTO Customers (FirstName, LastName, StreetAddress, City, PostalCode) " +
                    "VALUES (@firstname, @lastname, @streetaddress, @city, @postalcode)";

                command.Parameters.AddWithValue("@firstname", newUser.FirstName);
                command.Parameters.AddWithValue("@lastname", newUser.LastName);
                command.Parameters.AddWithValue("@streetaddress", newUser.StreetAddress);
                command.Parameters.AddWithValue("@city", newUser.City);
                command.Parameters.AddWithValue("@postalcode", newUser.PostalCode);
                command.ExecuteNonQuery();
            }
        }
    }
}

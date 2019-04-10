using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
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
    /// Interaction logic for AddMovieWindow.xaml
    /// </summary>
    public partial class AddMovieWindow : Window
    {
        public AddMovieWindow()
        {
            InitializeComponent();
        }

        private void AddMovieCancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddMovieButton_Click(object sender, RoutedEventArgs e)
        {
            AddMovieParameters movieData = new AddMovieParameters();
            if (MandatoryFieldsFilled(movieData))
            {
                string connectionString = ConfigurationManager.ConnectionStrings["VideoKlub.Properties.Settings.VideoKlubMarkoConnectionString"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = connection.CreateCommand())
                {
                    connection.Open();
                    command.CommandText = "INSERT INTO Movies (Title, Director, NumberOfCopies, Year, Runtime) " +
                        "VALUES (@title, @director, @numberofcopies, @year, @runtime)";

                    command.Parameters.AddWithValue("@title", movieData.Title);
                    if (movieData.Director==null)
                        command.Parameters.AddWithValue("@director", DBNull.Value);
                    else
                        command.Parameters.AddWithValue("@director", movieData.Director);
                    command.Parameters.AddWithValue("@numberofcopies", movieData.NumberOfCopies);
                    command.Parameters.AddWithValue("@year", movieData.Year);
                    if (movieData.Runtime!=null)
                        command.Parameters.AddWithValue("@runtime", movieData.Runtime);
                    else
                        command.Parameters.AddWithValue("@runtime", DBNull.Value);
                    command.ExecuteNonQuery();
                }
                MessageBox.Show($"Movie {movieData.Title} added to the database.");
                this.Close();
            }
        }

        private bool MandatoryFieldsFilled(AddMovieParameters data)
        {
            int movieYear;
            int numberOfCopies;
            int movieRuntime;
            if (String.IsNullOrWhiteSpace(titleTextbox.Text))
            {
                MessageBox.Show("Please enter a valid title!");
                return false;
            }
            else if (!(int.TryParse(yearTextbox.Text, out movieYear)))
            {
                MessageBox.Show("Please enter a valid year!");
                return false;
            }
            else if (!(int.TryParse(copiesTextbox.Text, out numberOfCopies)))
            {
                MessageBox.Show("Please enter a valid number of copies!");
                return false;
            }

            data.Title = titleTextbox.Text;
            data.Year = movieYear;
            if (numberOfCopies <= 0)
            {
                MessageBox.Show("Number of copies must be larger than 0!");
                return false;
            }
            data.NumberOfCopies = numberOfCopies;
            if (runtimeTextbox.Text == "")
                data.Runtime = null;
            else
            {
                if (int.TryParse(runtimeTextbox.Text, out movieRuntime))
                    data.Runtime = movieRuntime;
                else
                    MessageBox.Show("Invalid runtime!");
            }
            if (!String.IsNullOrWhiteSpace(directorTextbox.Text))
                data.Director = directorTextbox.Text;
            else
                data.Director = null;
            return true;
        }
    }
}

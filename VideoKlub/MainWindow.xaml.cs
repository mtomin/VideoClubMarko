using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;
using System.Data;

namespace VideoKlub
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string connectionString = ConfigurationManager.ConnectionStrings["VideoKlub.Properties.Settings.VideoKlubMarkoConnectionString"].ConnectionString;
        SqlConnection connection;
        DataTable userSearchResultsTable = new DataTable();
        DataTable movieSearchResultsTable = new DataTable();
        DataTable rentedMoviesList = new DataTable();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void UserSearch(string searchQuery)
        {
            //Search users by string
            string query = "SELECT " +
                "CustomerID, FirstName, LastName, StreetAddress, City, PostalCode, Movie1, Movie2, Movie3, mov1.Title as Title1, mov2.Title as Title2, mov3.Title as Title3 " +
                "from Customers cust LEFT JOIN movies mov1 on cust.Movie1 = mov1.movieId LEFT JOIN movies mov2 on cust.Movie2 = mov2.movieId LEFT JOIN movies mov3 on cust.Movie3 = mov3.movieId " +
                "WHERE LastName LIKE @userSearchQuery + '%'";
            
            using (connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                command.Parameters.AddWithValue("@userSearchQuery", searchQuery);
                userSearchResultsTable = new DataTable();
                adapter.Fill(userSearchResultsTable);
                resultsListboxUsers.DataContext = userSearchResultsTable;
            }
        }

        private void UserSearch(int userID)
        {
            //Search users by userID
            string query = "SELECT " +
                "CustomerID, FirstName, LastName, StreetAddress, City, PostalCode, Movie1, Movie2, Movie3, mov1.Title as Title1, mov2.Title as Title2, mov3.Title as Title3 " +
                "from Customers cust LEFT JOIN movies mov1 on cust.Movie1 = mov1.movieId LEFT JOIN movies mov2 on cust.Movie2 = mov2.movieId LEFT JOIN movies mov3 on cust.Movie3 = mov3.movieId " +
                "WHERE CustomerID=@userSearchQuery";

            using (connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                command.Parameters.AddWithValue("@userSearchQuery", userID);
                userSearchResultsTable = new DataTable();
                adapter.Fill(userSearchResultsTable);

                resultsListboxUsers.DataContext = userSearchResultsTable;
            }
        }

        private void SearchUserButton_Click(object sender, RoutedEventArgs e)
        {
            string searchQuery = searchBar.Text;
            UserSearch(searchQuery);
        }

        private void ShowSelectedUserDetails(object sender, SelectionChangedEventArgs e)
        {
            if (resultsListboxUsers.SelectedIndex >= 0)
            {
                int selectionIndex = resultsListboxUsers.SelectedIndex;
                //int selectedUserID = userSearchResultsTable.Rows[resultsListboxUsers.SelectedIndex].Field<int>(0);
                DataTable tempDatatable = userSearchResultsTable.Clone();
                tempDatatable.ImportRow(userSearchResultsTable.Rows[resultsListboxUsers.SelectedIndex]);
                userDetailsListbox.DataContext = tempDatatable;
                userDetailsListbox.Items.Refresh();

                //Show rented movies
                rentedMoviesList = new DataTable();
                rentedMoviesList.Columns.Add("Title");
                rentedMoviesList.Columns.Add("MovieId");
                   
                if (userSearchResultsTable.Rows[selectionIndex]["Title1"]!=DBNull.Value)
                {
                    rentedMoviesList.Rows.Add(new object[] { userSearchResultsTable.Rows[selectionIndex]["Title1"], userSearchResultsTable.Rows[selectionIndex]["Movie1"] });
                }
                if (userSearchResultsTable.Rows[selectionIndex]["Title2"]!= DBNull.Value)
                {
                    rentedMoviesList.Rows.Add(new object[] { userSearchResultsTable.Rows[selectionIndex]["Title2"], userSearchResultsTable.Rows[selectionIndex]["Movie2"] });
                }
                if (userSearchResultsTable.Rows[selectionIndex]["Title3"]!= DBNull.Value)
                {
                    rentedMoviesList.Rows.Add(new object[] { userSearchResultsTable.Rows[selectionIndex]["Title3"], userSearchResultsTable.Rows[selectionIndex]["Movie3"] });
                }
                
                rentedMoviesListbox.DataContext = rentedMoviesList;
                rentedMoviesListbox.Items.Refresh();
            }
        }

        private void ShowSelectedMovieDetails(object sender, SelectionChangedEventArgs e)
        {
            //Display director, runtime and number of copies
            if (resultsListboxMovies.SelectedIndex >= 0)
            {
                DataTable tempDatatable = movieSearchResultsTable.Clone();
                tempDatatable.ImportRow(movieSearchResultsTable.Rows[resultsListboxMovies.SelectedIndex]);
                movieDetailsListbox.DataContext = tempDatatable;
                movieDetailsListbox.Items.Refresh();
            }
        }

        private void RentMovie(int userID, int movieID)
        {
            //do the renting - update the database
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = "UPDATE Customers " +
                    "SET Movie1 = CASE WHEN (Movie1 is NULL) THEN @rentedMovie ELSE Movie1 END, " +
                    "Movie2 = CASE WHEN ((Movie1 IS NOT NULL) AND (Movie2 IS NULL)) THEN @rentedMovie ELSE Movie2 END, " +
                    "Movie3 = CASE WHEN ((Movie1 IS NOT NULL) AND (Movie2 IS NOT NULL) AND (Movie3 IS NULL)) THEN @rentedMovie ELSE Movie3 END " +
                    "WHERE CustomerID=@currentUser";

                command.CommandText += " UPDATE Movies " + "SET NumberOfCopies-=1 WHERE MovieID=@rentedMovie";
                command.Parameters.AddWithValue("@rentedMovie", movieID);
                command.Parameters.AddWithValue("@currentUser", userID);
                command.ExecuteNonQuery();
            }
        }

        private bool CheckSelectionValidity(int userSelectionIndex, int movieSelectionIndex)
        {
            //Check if both user and movie have been selected
            if (userSelectionIndex < 0)
            {
                MessageBox.Show("No user selected!");
                return false;
            }
            else if (movieSelectionIndex < 0)
            {
                MessageBox.Show("No movie selected");
                return false;
            }
            return true;
        }

        private bool CheckCanRent(DataTable userTable, DataTable movieTable, int userSelectionIndex)
        {
            //Get data for Messagebox and checking if there is >0 copies of movie available for rent
            string userFirstName = (string)userSearchResultsTable.Rows[userSelectionIndex]["FirstName"];
            string userLastName = (string)userSearchResultsTable.Rows[userSelectionIndex]["LastName"];
            string movieName = (string)movieSearchResultsTable.Rows[resultsListboxMovies.SelectedIndex]["Title"];
            int numberOfCopies = (int)movieSearchResultsTable.Rows[resultsListboxMovies.SelectedIndex]["NumberOfCopies"];
            int movieId = (int)movieSearchResultsTable.Rows[resultsListboxMovies.SelectedIndex]["MovieId"];

            //Check how many movies user currently has rented
            //Field method used due to issues with casting DBNull into nullable type
            int? userMovie1 = (int?)userSearchResultsTable.Rows[userSelectionIndex].Field<int?>("Movie1");
            int? userMovie2 = (int?)userSearchResultsTable.Rows[userSelectionIndex].Field<int?>("Movie2");
            int? userMovie3 = (int?)userSearchResultsTable.Rows[userSelectionIndex].Field<int?>("Movie3");

            if (MessageBox.Show(String.Format("Are you sure {0} {1} wants to rent {2}?", userFirstName, userLastName, movieName), "Rent movie?", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                //Check if movie available
                if (numberOfCopies == 0)
                {
                    MessageBox.Show("No physical copies of the movie currently available in store!");
                    return false;
                }
                //Check if user has rented maximum number of movies
                else if (userMovie1 != null && userMovie2 != null && userMovie3 != null)
                {
                    MessageBox.Show("The customer has rented the maximum amount of movies (3)");
                    return false;
                }
                else if (userMovie1==movieId || userMovie2==movieId || userMovie3==movieId)
                {
                    MessageBox.Show("The customer has already rented a copy of that movie!");
                    return false;
                }
                else
                    return true;
            }
            else
                return false;
        }

        private void RentMovieButton_Click(object sender, RoutedEventArgs e)
        {
            //Check if both user and movie have been selected
            int userSelectionIndex = resultsListboxUsers.SelectedIndex;
            int movieSelectionIndex = resultsListboxMovies.SelectedIndex;
            if (!CheckSelectionValidity(userSelectionIndex, movieSelectionIndex))
                return;

            //Get info on current selection for easier readability
            int selectedUserID = (int)userSearchResultsTable.Rows[userSelectionIndex]["CustomerID"];
            int selectedMovieID = (int)movieSearchResultsTable.Rows[movieSelectionIndex]["MovieId"];
            int numberOfCopies = (int)movieSearchResultsTable.Rows[resultsListboxMovies.SelectedIndex]["NumberOfCopies"];
            
            //Get info on currently rented movies to check if the user has rented the maximum limit
            if (CheckCanRent(userSearchResultsTable, movieSearchResultsTable, userSelectionIndex))
            {
                //Update database, show that the number of available copies is reduced by one (just manually change query results instead of making another query)
                RentMovie(selectedUserID, selectedMovieID);
                movieSearchResultsTable.Rows[resultsListboxMovies.SelectedIndex].SetField("NumberOfCopies", numberOfCopies - 1);
                ShowSelectedMovieDetails(null, null);

                //Do a user query to pull the data on rented movies
                UserSearch(selectedUserID);
                resultsListboxUsers.SelectedIndex=0;
                ShowSelectedMovieDetails(null, null);
                ShowSelectedUserDetails(null, null);
            }
        }

        private void MovieSearch()
        {
            string query = "SELECT * from Movies WHERE Title LIKE @userSearchQuery + '%'";

            using (connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                command.Parameters.AddWithValue("@userSearchQuery", searchBar.Text);

                movieSearchResultsTable = new DataTable();
                adapter.Fill(movieSearchResultsTable);
            }
            resultsListboxMovies.DataContext = movieSearchResultsTable;
            resultsListboxMovies.Items.Refresh();
        }

        private void ReturnMovie_Click(object sender, RoutedEventArgs e)
        {
            int userSelectionIndex = resultsListboxUsers.SelectedIndex;
            int movieSelectionIndex = rentedMoviesListbox.SelectedIndex;
            if (CheckSelectionValidity(userSelectionIndex, movieSelectionIndex))
            {
                string userFirstName = (string)userSearchResultsTable.Rows[userSelectionIndex]["FirstName"];
                string userLastName = (string)userSearchResultsTable.Rows[userSelectionIndex]["LastName"];
                string movieName = (string)rentedMoviesList.Rows[movieSelectionIndex]["Title"];
                int returnMovieID = int.Parse(rentedMoviesList.Rows[movieSelectionIndex]["MovieId"].ToString());
                int returnUserID = (int)userSearchResultsTable.Rows[userSelectionIndex]["CustomerID"];
                if (MessageBox.Show(String.Format("{0} {1} wants to return the movie {2}. Proceed?", userFirstName, userLastName, movieName), "Rent movie?", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    ReturnMovie(returnUserID, returnMovieID);
            }
        }

        private void ReturnMovie(int returnUserID, int returnMovieID)
        {

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = "UPDATE Customers " +
                    "SET Movie1 = CASE WHEN Movie1=@returnedMovie THEN NULL ELSE Movie1 END, " +
                    "Movie2 = CASE WHEN Movie2 =@returnedMovie THEN NULL ELSE Movie2 END, " +
                    "Movie3 = CASE WHEN Movie3 =@returnedMovie THEN NULL ELSE Movie3 END " +
                    "WHERE CustomerID=@currentUser";

                command.CommandText += " UPDATE Movies " + "SET NumberOfCopies+=1 WHERE MovieID=@returnedMovie";
                command.Parameters.AddWithValue("@returnedMovie", returnMovieID);
                command.Parameters.AddWithValue("@currentUser", returnUserID);
                command.ExecuteNonQuery();

                //Update database, show that the number of available copies is reduced by one (just manually change query results instead of making another query)
                
                ShowSelectedMovieDetails(null, null);

                //Do a user query to pull the data on rented movies
                UserSearch(returnUserID);
                resultsListboxUsers.SelectedIndex = 0;

                ShowSelectedMovieDetails(null, null);
                ShowSelectedUserDetails(null, null);
            }
        }

            private void SearchMovieButton_Click(object sender, RoutedEventArgs e)
        {
            MovieSearch();
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            AddUserWindow addUserWindow = new AddUserWindow();
            addUserWindow.Show();
        }

        private void AddMovieButton_Click(object sender, RoutedEventArgs e)
        {
            AddMovieWindow addMovieWindow = new AddMovieWindow();
            addMovieWindow.Show();
        }

        private void RemoveMovieButton_Click(object sender, RoutedEventArgs e)
        {
            int movieSelectionIndex = resultsListboxMovies.SelectedIndex;
            if (movieSelectionIndex>=0)
            {
                string movieName = movieSearchResultsTable.Rows[movieSelectionIndex].Field<string>(1);
                int selectedMovieID = movieSearchResultsTable.Rows[movieSelectionIndex].Field<int>(0);
                if (MessageBox.Show(String.Format("Are you sure you want to remove the movie {0} from database? (MovieID={1})", movieName, selectedMovieID.ToString()), "Delete movie?", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    using (SqlCommand command = connection.CreateCommand())
                    {
                        connection.Open();
                        command.CommandText = "DELETE FROM Movies WHERE MovieID=@deletedMovieId";
                        command.Parameters.AddWithValue("@deletedMovieId", selectedMovieID);
                        command.ExecuteNonQuery();
                        movieSearchResultsTable.Rows.RemoveAt(movieSelectionIndex);
                        resultsListboxMovies.Items.Refresh();
                    }
                }
            }
        }
    }
}
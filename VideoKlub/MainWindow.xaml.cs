using System;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Configuration;
using System.Data;

namespace VideoKlub
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly string connectionString = ConfigurationManager.ConnectionStrings["VideoKlub.Properties.Settings.VideoKlubMarkoConnectionString"].ConnectionString;
        SqlConnection connection;
        DataTable userSearchResultsTable = new DataTable();
        DataTable movieSearchResultsTable = new DataTable();
        DataTable rentedMoviesList = new DataTable();

        const int MAX_MOVIES_RENTED = 3;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void UserSearch(string searchQuery)
        {
            //Search users by string
            string query = "SELECT " +
                "CustomerID, FirstName, LastName, StreetAddress, City, PostalCode " +
                "from Customers cust " +
                "WHERE LastName LIKE @userSearchQuery + '%'";
            
            using (SqlConnection connection = new SqlConnection(connectionString))
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
                "CustomerID, FirstName, LastName, StreetAddress, City, PostalCode " +
                "from Customers cust " +
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
                int selectedUserID = userSearchResultsTable.Rows[resultsListboxUsers.SelectedIndex].Field<int>(0);
                DataTable tempDatatable = userSearchResultsTable.Clone();
                tempDatatable.ImportRow(userSearchResultsTable.Rows[resultsListboxUsers.SelectedIndex]);
                userDetailsListbox.DataContext = tempDatatable;
                userDetailsListbox.Items.Refresh();

                //Show rented movies
                string query = "SELECT "+
                                "Title, MovieId " +
                                "FROM Customers cust LEFT JOIN CustomerMovies ctom on cust.CustomerID = ctom.Customer LEFT JOIN Movies on ctom.Movie = Movies.MovieId " +
                                "WHERE cust.CustomerID=@selectedUserID";
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    rentedMoviesList = new DataTable();
                    command.Parameters.AddWithValue("@selectedUserID", selectedUserID);
                    adapter.Fill(rentedMoviesList);
                }

                rentedMoviesListbox.DataContext = rentedMoviesList;
                rentedMoviesListbox.Items.Refresh();
            }
        }

        private void ShowSelectedMovieDetails(object sender, SelectionChangedEventArgs e)
        {
            //Display director, runtime and number of copies by creating a new table and binding it to movieDetailsListbox
            if (resultsListboxMovies.SelectedIndex >= 0)
            {
                DataTable singleMovieData = movieSearchResultsTable.Clone();
                singleMovieData.ImportRow(movieSearchResultsTable.Rows[resultsListboxMovies.SelectedIndex]);
                movieDetailsListbox.DataContext = singleMovieData;
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
                command.CommandText = "INSERT INTO CustomerMovies (Customer, Movie, DateRented) " +
                    "VALUES (@customerID, @movieID, @DateRented)";

                command.CommandText += " UPDATE Movies " + "SET NumberOfCopies-=1 WHERE MovieID=@movieID";
                command.Parameters.AddWithValue("@customerID", userID);
                command.Parameters.AddWithValue("@movieID", movieID);
                command.Parameters.AddWithValue("@DateRented", DateTime.Now);
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

            if (MessageBox.Show(String.Format("Are you sure {0} {1} wants to rent {2}?", userFirstName, userLastName, movieName), "Rent movie?", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                //Check if movie available
                if (numberOfCopies == 0)
                {
                    MessageBox.Show("No physical copies of the movie currently available in store!");
                    return false;
                }
                //Check if user has rented maximum number of movies
                else if (rentedMoviesList.Rows.Count == MAX_MOVIES_RENTED)
                {
                    MessageBox.Show(String.Format("The customer has rented the maximum amount of movies ({0})", MAX_MOVIES_RENTED));
                    return false;
                }
                //Check if user has already rented the movie
                else if (rentedMoviesList.AsEnumerable().Any(row => movieId == row.Field<int?>("MovieId")))
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

            using (SqlConnection connection = new SqlConnection(connectionString))
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
            const double PRICE_PER_DAY = 12.00;
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = connection.CreateCommand())
            {
                //Pull data on rent date from database and calculate the price
                command.CommandText = "SELECT " +
                    "TOP 1 (DateRented) " +
                    "FROM CustomerMovies " +
                    "WHERE " +
                    "Customer=@returnUserID AND Movie=@returnMovieID";
                command.Parameters.AddWithValue("@returnMovieID", returnMovieID);
                command.Parameters.AddWithValue("@returnUserID", returnUserID);

                connection.Open();
                DateTime rentDate = (DateTime)command.ExecuteScalar();
                double rentCost = ((DateTime.Now - rentDate).Days+1)*PRICE_PER_DAY;

                MessageBox.Show(String.Format("The movie was rented on {0}.\nTotal cost for this rent period is {1} HRK", rentDate.ToString("MM/dd/yyyy"), String.Format("{0:0.00}", rentCost)), "COST OF RENTAL");
                
                //Update database
                command.CommandText = "DELETE FROM CustomerMovies " +
                    "WHERE " +
                    "Customer=@returnUserID AND Movie=@returnMovieID";

                command.CommandText += " UPDATE Movies " + 
                    "SET NumberOfCopies+=1 " +
                    "WHERE MovieId=@returnMovieID";

                command.ExecuteNonQuery();

                //Show that the number of available copies is increased by one if the movie is currently selected (just manually change query results instead of making another query)
                foreach (DataRow row in movieSearchResultsTable.Rows)
                {
                    if ((int)row["MovieId"] == returnMovieID)
                    {
                        int numberOfCopies = (int)row["NumberOfCopies"];
                        numberOfCopies++;
                        row.SetField("NumberOfCopies", numberOfCopies);
                        ShowSelectedMovieDetails(null, null);
                        break;
                    }
                }
                
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
# VideoClubMarko

This is a simple app for video store management. Data on users and movies is stored in a SQL database, while the user interface is made in WPF. It implements adding new users as well as adding and removing movies. 

## User interface

A single search bar is used for searching both movies and users. For simpler testing, searching for an empty string returns all users/movies. Upon selection, user and movie details are shown. 
![alt text](https://i.imgur.com/i1FoH7a.png "User interface")


Renting movies is handled via an association table. When a single user is selected, his rented movies are listed. On return, a messagebox informs us of the rental date and the total cost. 

![alt text](https://i.imgur.com/T2rq44b.png "Returning a movie")

### Author's notes

This app was built using solely [ADO.NET](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/ado-net-overview) for two reasons:

- While learning how to work with databases, the simplest approach was chosen
- Manual mapping of values has made me immensely more appreciative of ORMs

License
----

MIT

**Free Software, Hell Yeah!**
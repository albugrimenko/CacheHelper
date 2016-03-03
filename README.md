# CacheHelper
A simple helper class to maintain object type specific cache. It supports “remote” mode and allows to keep cached objects in a database (Microsoft SQL Server), which makes it accessible from multiple servers simultaneously.

### The original author
I have lost references to the original author of c# code. I remember that I found this project on CodeProject.com, but could not find that particular article... I apologize for that.

### Major Modifications
I did minor modifications to the code and mostly was interested in adding ability to store cached objects in a SQL Server database. Therefore, my major contribution is remote functionality:

* CacheDictionaryRemote.cs
* CacheDictionaryConcurRemote.cs
* \Helpers
* SQL Server Scripts

### SQL Server Scripts
There are 2 sets of scripts included: 

* for in-memory databases (gives better performance)
* for SQL Express, whcih does not support in memory databases

In-memory databases supported only by Enterprise version of Microsoft SQL Server. However, acceptable performance can be achieved even in SQL Express installation, where all data will be stored in a regular database. In this case, it is extremely important to optimize original databse size to the max number of objects stored during the day - the best performance could be achieved if databse size is constant and SQL Server does not have to grow data or log files.

IMPORTANT: to achive better performance, SQL Server does NOT monitor cached objects expiration time, therefore expired objects must be removed from time to time. There are two stored procedure calls could be used for that:

* **exec Object_DeleteExpired** - deletes all expired objects
* **exec Object_DeleteExpired	@IsCompleteReset = 1** - deletes all objects from the database. This stored procedure is useful to reset the cache database. 

Based on a particular cache load, the schedule could be optimized to clean up all expired objects by running *Object_DeleteExpired* every 1, 2, 3... hours. In many instances pretty good results could be achieved with just resetting the cache once a day.






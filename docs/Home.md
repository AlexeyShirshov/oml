**Project Description**
Yet another .NET-oriented ready to use object-relational mapping library. The key features are Web-related (caching, integration with standard ASP.NET providers).
The project is strated in early 2004. Now it's a proved framework for large and complex data-driven projects and web 

# Worm
[Worm](http://wise-orm.com/) is a .NET-oriented new truly object-relational mapping library which hides from the developer all SQL and pseudo-SQL statements. It supports CRUD, caching, undo, and other features. [Differencies](-NHibernate) from NHibernate. See also [Quick starts](Quick-starts) and [class reference](http://wise-orm.com/reference).

# Features.
* CRUD - Create, Read, Update and Delete
* [Resultset caching](Resultset-caching)
* One to many, many to many [relations](relations)
* Edit and undoing changes in memory
* Automatic dependency resolver during object graph saving
* Transactions
* Independence from concrete database schema and DBMS
* [Partial loading](Partial-loading)
* [Entity and commands lazy loading](Entity-and-commands-lazy-loading)
* [Utilities](Utilities) for automated wrappers classes creation from a database schema and from an XML file.
* [ASP.NET providers](ASP.NET-providers) implementation
###### See also [Basic features](Basic-features) and [Advanced features](Advanced-features)

# Quick start
Consider the following table "Store"
![](Home_http://wise-orm.com/image.axd?picture=2009%2f4%2fstore.PNG)
taked from standard AdventureWorks database. How can we map the table with a minimum cost to access the data from code?

Well first of all you should add reference to Worm.Orm.dll and CoreFramework.dll assemblies. The next thing - create Store class.
{code:c#}
public class Store
{
        public int CustomerID { get; set; }
        public string Name { get; set; }
}
{code:c#}
As you can see it just has two properties CustomerID and Name.

Third thing you should do is to add connection string to you database. For instance

Server=.\sqlexpress;Initial Catalog=AdventureWorks;Integrated security=true;

And the final step - writing a program. 
{code:c#}
static void Main(string[]() args)
{
    var query = new QueryCmd(exam1sharp.Properties.Settings.Default.connString);

    foreach (Store s in query
                .From(new SourceFragment("Sales", "Store"))
                .ToList())
    {
        Console.WriteLine("Store id: {0}, name: {1}", s.CustomerID, s.Name);
    }
}
{code:c#}
There is no attributes, XML files and other staff. Worm supports inline entity mapping.
* [How the data is retrieved](How-the-data-is-retrieved)
* [Relations (one 2 many)](Relations-(one-2-many))
* [Relations (many 2 many)](Relations-(many-2-many))
* [How to create, change and save objects](How-to-create,-change-and-save-objects)
* [How changes in objects affect relations](How-changes-in-objects-affect-relations)
## [Advanced features](Advanced-features)
* [Multi-table objects](Multi-table-objects)
* [Object on table-valued functions](Object-on-table-valued-functions)
* [Stored procedures](Stored-procedures)
* [ASP.NET providers](ASP.NET-providers)
* [How to use ASP.NET cache](How-to-use-ASP.NET-cache)
* [Schema version and object model evolution](Schema-version-and-object-model-evolution)
## [General thoughts](General-thoughts)
* [Memory, performance and expressions](Memory,-performance-and-expressions)
* [Non-relational schemas](Non-relational-schemas)
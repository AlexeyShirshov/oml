## Quick starts
See [blog](http://wise-orm.com).
Here is a list of topics covering quick start solutions.
### Create your own Worm-enabled solution
#### 1. Create a class library project
#### 2. Add a reference to worm.dll. You can get the latest stable version from Releases
#### 3. Generate xml object definitions using [XmlSchemaGen](Utilities) utility (see Releases). For example, 
{{ XmlSchemaGen.exe -O=test.xml -S=.\sqlexress -E -D=test. }}
See test [xml file](Quick starts_test.xml).
#### 4. Generate classes using [OrmCodeGen](Utilities) unitily (see Releases). For example, 
{{ OrmCodeGen.exe -f=test.xml -l=vb -o="my classes" }}
See test [VB.NET file](Quick starts_Albums.vb)
#### 5. Add generated classes into the project
That's all. Now you can write your own specific code implementing business logic without thinking about the database, object persistence and other stuff.
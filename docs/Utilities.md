## XML object definitions
If you prefer clean and transparent code you can write the whole code manually. It's really not so tedious and complicated. But if you have more than 10 entities, it might be wearisome that is where xml and code generators arise.
### OrmCodeGen
The utility helps to create a code from an XML object definition. Here are the options
{{
Command line parameters:
  -f    - source xml file
  -l    - code language [cs, vb](cs,-vb) ("cs" by default)
  -p    - generate partial classes ("false" by default)
  -sp   - split entity class and entity's schema definition
                  class code by diffrent files ("false" by default)
  -sk   - skip entities
  -e    - entities to process
  -cB   - behaviour of class codegenerator
                  [Objects, PartialObjects](Objects,-PartialObjects) ("Objects" by default)
  -sF   - create folder for each entity.
  -o    - output files folder.
  -pmp  - private members prefix ("m_" by default)
  -cnP  - class name prefix (null by default)
  -cnS  - class name suffix (null by default)
  -fnP  - file name prefix (null by default)
  -fnS  - file name suffix (null by default)
}}
It's pretty flexible, isn't it? But what is an xml object definitions? Is it a file I have to write manually? Well, you can. But it is better to generate it from a database. This is the work of XmlSchemaGen.
### XmlSchemaGen
This utility is intended for generating xml object definitions from databases. Here are the options
{{
Command line parameters
  -O=value      -  Output file name. Example: -O=test.xml. Default is <server>.xml
  -S=value      -  Database server. Example: -S=(local). Default is (local).
  -E            -  Integrated security.
  -U=value      -  Username
  -P=value      -  Password. Will requested if need.
  -D=value      -  Initial catalog(database). Example: -D=test
  -M=[msft](msft)     -  Manufacturer. Example: -M=msft. Default is msft.
  -schemas=list -  Database schema filter. Example: -schemas="dbo,one"
  -name=value   -  Database table name filter. Example: -name=aspnet_%; -name=!aspnet_%
  -F=[error](merge)      -  Existing file behavior. Example: -F=error. Default is merge.
  -R            -  Drop deleted columns. Meaningfull only with merge behavior. Example: -R.
  -N=value      -  Objects namespace. Example: -N=test.
  -Y            -  Unify entyties with the same PK. Example: -Y.
}}
If a target XML file exists where are two behaviours. Merge behaviour is the first and default one - the utility just adds new columns and tables to xml and doesn't touch existing definitions. With error behaviour the utility just throws an error when the target file exists.
### XSD
So, the core of autogenerating process is an xml object definitions file. It's a xml file with a certain structure. The structure is defined as [XSD](Utilities_OrmObjectsSchema.xsd).
You can edit xml definitions in various ways, but you should watch for XSD compliance. The test xml object definitions file might be found in Releases.
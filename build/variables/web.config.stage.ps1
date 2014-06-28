function Get-WebConfigVars {
    return $vars = @{
        Configuration = @{
            ConnectionStrings = @{
                MyContext = 'Data Source=CPHS-SQLTEST-01;Initial Catalog=COMPASS_CORE_QA;User ID=crm_usr;Password=cRmu5r'
                Compass_CORE = 'Data Source=CPHS-SQLTEST-01;Initial Catalog=COMPASS_CORE_QA;User ID=crm_usr;Password=cRmu5r'
                CompassDataWarehouse = 'Data Source=CPHS-SQLTEST-01;Initial Catalog=Compass_Data_Warehouse;User ID=crm_usr;Password=cRmu5r'
            }
            'System.Web' = @{
                CustomErrors = 'Off'
                Compilation = @{
                    Debug = 'false'
                }
            }
            AppSettings = @{
                VirtualDirectoryName = 'Planviewer'
            }
        }
    }
}

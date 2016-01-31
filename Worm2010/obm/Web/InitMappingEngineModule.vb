#If weblazyload Then
Imports Microsoft.Web.Infrastructure.DynamicModuleHelper
#End If
Imports System.Web.Hosting

Namespace Web
    Public Class InitMappingEngineModule
        Implements IHttpModule
        Private _spin As New SpinLockRef
        Private _called As Boolean
        Public Sub Dispose() Implements IHttpModule.Dispose
            
        End Sub

        Public Sub Init(context As HttpApplication) Implements IHttpModule.Init
            If Not _called Then
                Using New CSScopeMgrLite(_spin)
                    If Not _called Then
                        _called = True
                        ObjectMappingEngine.StartLoadEntityTypes()
                    End If
                End Using
            End If
        End Sub

        Public Shared Sub RegisterModule()
            If HostingEnvironment.IsHosted Then
#If weblazyload Then
                DynamicModuleUtility.RegisterModule(GetType(InitMappingEngineModule))
#End If
            Else
                ObjectMappingEngine.StartLoadEntityTypes()
            End If
        End Sub
    End Class
End Namespace
<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="SolutionsTools.UI.Login" %>

<!DOCTYPE html>

<html lang="en" xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Partner Solution</title>

    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1, shrink-to-fit=no" />

    <link rel="icon" href="Img/favicon.png"/>

    <link href="https://fonts.googleapis.com/css?family=Lato:300,400,700&display=swap" rel="stylesheet" />

    <link rel="stylesheet" href="https://stackpath.bootstrapcdn.com/font-awesome/4.7.0/css/font-awesome.min.css" />

    <link rel="stylesheet" href="content/style.css" />
    
    <link rel="stylesheet" href="content/bootstrap.min.css" />
    
    <link rel="stylesheet" href="content/loader.css" />

    <script src="//code.jquery.com/jquery-3.5.1.js"></script>

    <script type="text/javascript"> 
        $(document).on("keypress", "input.form-control", function (e) {
            if (e.which == 13) {
                $('#btnLogin').click();
            }
        });
        function showAlert(msg) {
            var alert = document.querySelector('.alert-info');

            alert.innerHTML = msg;

            $('#show-alert').fadeIn(1000);

            setTimeout(function () {
                $('#show-alert').fadeOut(1000);
            }, 7000);
        }
    </script>
</head>
<body>
    <section class="ftco-section">
        <div class="container">
            <div class="row justify-content-center">
                <div class="col-md-6 text-center mb-5">
                    <h2 class="heading-section"></h2>
                </div>
            </div>
            <div class="row justify-content-center">
                <div class="col-md-7 col-lg-5">
                    <div class="login-wrap p-4 p-md-5">
                        <div style="text-align: center; padding: 20px">
                            <span><img alt="" src="Img/logo-color.png" /></span>
                        </div>

                        <h3 class="text-center mb-4">
                            <label runat="server" id="labelFunction">Login</label>
                        </h3>
                        
                        <form class="login-form" runat="server">
                            <div class="form-group">
                                <input type="text" class="form-control rounded-left" placeholder="Usuário" required="required" runat="server" id="inputUsername" />
                            </div>
                            <div class="form-group d-flex">
                                <input type="password" class="form-control rounded-left" placeholder="Senha" required="required" runat="server" id="inputPassword" />
                            </div>
                            <div class="form-group d-flex">
                                <input type="password" class="form-control rounded-left" placeholder="Confirmar senha" required="required" runat="server" id="inputPasswordConfirm" visible="false"/>
                            </div>
                            <div class="form-group">
                                <button type="button" class="form-control btn btn-dark rounded submit px-3" runat="server" id="btnLogin" onserverclick="btnLogin_Click">Entrar</button>
                            </div>
                            <div class="form-group d-md-flex">
                                <div class="w-50">
                                    <label class="checkbox-wrap checkbox-primary">
                                    </label>
                                </div>
                                <div class="w-50 text-md-right">
                                    <asp:LinkButton class="text-dark" runat="server" Text="Recuperar senha" OnClick="btnRecovery_Click" ></asp:LinkButton>
                                </div>
                            </div>
                            
                            <div class="alert alert-info" style="display:none;" id="show-alert">
                                <span class="alert-info" id="info">As senhas digitadas não coincidem!</span>
                            </div>

                            <input type="hidden" runat="server" id="change_function" value="N"/>
                        </form>
                    </div>
                </div>
            </div>
        </div>
    </section>

    <script type="text/javascript" src="Scripts/popper.min.js"></script>
    <script type="text/javascript" src="Scripts/bootstrap.min.js"></script>
    <script type="text/javascript" src="Scripts/main.js"></script>
    <script type="text/javascript" src="Scripts/loader.js"></script>

</body>
</html>
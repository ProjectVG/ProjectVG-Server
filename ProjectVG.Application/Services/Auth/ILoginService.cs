using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectVG.Application.Services.Auth
{
    internal interface ILoginService
    {

        public void Login(LoginRequest request);

        public void Logout();

        public void Signup();

    }


    class LoginRequest
    {
        public string Provider { get; set; }
        public string ProviderId { get; set; }

    }



}

using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Application.Auth.Login;

public record LoginCommand(string Email, string Password);

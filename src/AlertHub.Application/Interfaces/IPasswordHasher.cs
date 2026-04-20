using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Application.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

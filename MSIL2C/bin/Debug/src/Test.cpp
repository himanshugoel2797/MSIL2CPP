#include "Test.h"
void Test::Program::Main()
{
    int32 V_0 = 0x64;
    string V_1 = "lol {0}";
    (void)  System::Console::Write(V_1,V_0);
    string v_0 = V_0.ToString();
    (void)  System::Console::Write(v_0,V_1);
}

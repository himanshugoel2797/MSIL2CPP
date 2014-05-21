#include "Test.h"
void Test::Program::Main()
{
    int32 V_0 = 0x64;
    string V_1 = "lol {0}";
    (void)  System::Console::Write(V_1);
    string v_0 = V_0.ToString();
    string V_2 = v_0;
    string v_1 = " ".Concat(V_0);
    V_2 = V_2;
    (void)  System::Console::Write(v_1);
}

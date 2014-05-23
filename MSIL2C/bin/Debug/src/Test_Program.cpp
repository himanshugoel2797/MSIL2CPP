#include "Test_Program.h"
System::Void Test::Program::main()
{
    System::Int32 V_0;
    System::String V_1;
    System::Int64 V_2;
    System::UInt32 V_3;
    System::Int16 V_4;
    System::Boolean V_5;
    V_0 = 100;
    V_1 = "lol {0}";
    V_2 = 10;
    V_3 = 10;
    V_4 = 10;
    bool v_0 = (99 < V_0);
    bool v_1 = (0 == v_0);
    V_5 = v_1;
    if (V_5 != 0)
    {
        System::Console::Write(V_1);
    }
    else
    {
        V_2 = 50;
        System::Console::Write(V_1);
    }

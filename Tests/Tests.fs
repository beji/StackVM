namespace Tests

open NUnit.Framework
open StackVM

module InstructionTests =

    [<Test>]
    let ``Push 4 pushes 4 on the stack`` () =
        let result = 
            [Push 4]
            |> Stack.fold Stack.initialState
        Assert.AreEqual(List.head result, 4)
    [<Test>]
    let ``Pop removes the head of the stack `` () =
        let result = 
            [Pop]
            |> Stack.fold [4;3;7]
        Assert.AreEqual(result, [3;7])
    [<Test>]
    let ``Add calculates the sum of 2 numbers`` () =
        let result = 
            [Push 4; Push 3; Add]
            |> Stack.fold Stack.initialState
        Assert.AreEqual(List.head result, 7)

    [<Test>]
    let ``Subtract subtracts`` () =
        let result = 
            [Push 4; Push 3; Subtract]
            |> Stack.fold Stack.initialState
        Assert.AreEqual(List.head result, 1)

    [<Test>]
    let ``Multiply multiplies`` () =
        let result = 
            [Push 4; Push 2; Multipy]
            |> Stack.fold Stack.initialState
        Assert.AreEqual(List.head result, 8)    

    [<Test>]
    let ``Divide divides`` () =
        let result = 
            [Push 4; Push 2; Divide]
            |> Stack.fold Stack.initialState
        Assert.AreEqual(List.head result, 2)

    [<Test>]
    let ``Ignore does nothing`` () =
        let result = 
            [Ignore]
            |> Stack.fold Stack.initialState
        Assert.AreEqual(result, [])

    [<Test>]
    let ``Print doesn't modify the stack`` () =
        let result = 
            [Push 4; Print]
            |> Stack.fold Stack.initialState
        Assert.AreEqual(List.head result, 4)
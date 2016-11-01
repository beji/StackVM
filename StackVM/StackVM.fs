namespace StackVM

open System
open System.IO
open System.Text.RegularExpressions

[<AutoOpenAttribute>]
module Types =
    type Instruction =
        | Push of int
        | Pop
        | Add
        | Subtract
        | Divide
        | Multipy
        | Print
        | Ignore
    type Message = Instruction * AsyncReplyChannel<Instruction>

module AssemblyParser =

    type OrElseBuilder() =
        member this.ReturnFrom x = x
        member this.Combine (a,b) =
            match a with
            | Some _ -> a
            | None -> b
        member this.Delay f = f()

    let orElse = new OrElseBuilder()

    let simpleLineHelper str retval input =
        match input = str with
        | true -> Some retval
        | false -> None

    let getPrintLine = simpleLineHelper "print" Print
    let getAddLine = simpleLineHelper "add" Add
    let getSubtractLine = simpleLineHelper "sub" Subtract
    let getMultiplyLine = simpleLineHelper "mul" Multipy
    let getDivisionLine = simpleLineHelper "div" Divide
    let getPopLine = simpleLineHelper "pop" Pop

    let getPushLine input =
        if input = null then None
        else
            let m = Regex.Match(input, "push (\d+)")
            if m.Success then //Some(List.tail [ for g in m.Groups -> g.Value ])
                List.tail [ for g in m.Groups -> g.Value ]
                |> List.head
                |> System.Int32.Parse
                |> Push
                |> Some 
            else None

    let read = File.ReadAllLines
    let strip (lines : string list) =
        lines
        //Filter comments
        |> List.filter (fun line ->
            not <| line.StartsWith("#"))
        //Filter empty lines
        |> List.filter (fun line ->
            line = "" |> not)
        //Trim all lines
        |> List.map (fun line -> line.Trim())
    let parse (lines : string list) =
        printfn "parsing %A" lines
        lines
        |> List.map (fun line ->
            let statement = orElse{
                return! getPushLine line
                return! getAddLine line
                return! getSubtractLine line
                return! getDivisionLine line
                return! getMultiplyLine line
                return! getPrintLine line
                return! getPopLine line                
            }
            match statement with
            | Some x -> x
            | None -> Ignore)    

module Stack =
    let initialState = []

    let calcNewState currentState instruction =
            let dbgIdentifier = "[StackVM]:"
            match instruction with
            | Push x ->
                // Push x on top of the stack
                printfn "%s pushing %i on the stack" dbgIdentifier x
                x :: currentState
            | Pop ->
                // Pop the top of the stack
                printfn "%s popping from the stack" dbgIdentifier
                List.tail currentState
            | Add ->
                match currentState with
                | x::y::rst ->
                    printfn "%s adding %i and %i" dbgIdentifier x y
                    (y + x) :: rst
                | _ ->
                    printfn "%s something went horribly wrong with the stack while trying to add" dbgIdentifier
                    currentState
            | Subtract ->
                match currentState with
                | x::y::rst ->
                    printfn "%s subtracting %i and %i" dbgIdentifier x y
                    (y - x) :: rst
                | _ ->
                    printfn "%s something went horribly wrong with the stack while trying to subtract" dbgIdentifier
                    currentState
            | Divide ->
                match currentState with
                | x::y::rst ->
                    printfn "%s dividing %i and %i" dbgIdentifier x y
                    (y / x) :: rst
                | _ ->
                    printfn "%s something went horribly wrong with the stack while trying to divide" dbgIdentifier
                    currentState
            | Multipy ->
                match currentState with
                | x::y::rst ->
                    printfn "%s multiplying %i and %i" dbgIdentifier x y
                    (y * x) :: rst
                | _ ->
                    printfn "%s something went horribly wrong with the stack while trying to multipy" dbgIdentifier
                    currentState
            | Print ->
                printfn "%s printing the current head" dbgIdentifier
                printfn "%i" <| List.head currentState
                currentState
            | Ignore ->
                printfn "%s i do not know that instruction" dbgIdentifier 
                currentState

module Main =

    [<EntryPoint>]
    let main argv =
        printfn "%A" argv
        
        argv
        |> Array.iter (fun arg ->
            if File.Exists arg then
                printfn "parsing File %s" arg
                File.ReadAllLines arg
                |> Array.toList
                |> AssemblyParser.strip
                |> AssemblyParser.parse
                |> List.fold (fun stack instruction ->
                    Stack.calcNewState stack instruction
                ) Stack.initialState
                |> printfn "final stack: %A"
        ) 

        0 // return an integer exit code

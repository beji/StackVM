namespace StackVM

open System
open System.IO
open System.Text.RegularExpressions
open System.Runtime.Serialization
open System.Runtime.Serialization.Formatters.Binary

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

module Compiler =
    let serializeBinary<'a> (x :'a) =
        let binFormatter = new BinaryFormatter()

        use stream = new MemoryStream()
        binFormatter.Serialize(stream, x)
        stream.ToArray()

    let deserializeBinary<'a> (arr : byte[]) =
        let binFormatter = new BinaryFormatter()

        use stream = new MemoryStream(arr)
        binFormatter.Deserialize(stream) :?> 'a

    let serializeSasm = serializeBinary<Instruction list>
    let deserializeSasm = deserializeBinary<Instruction list>

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

    let fold state instructions =
        List.fold(fun stack instruction ->
            calcNewState stack instruction
        ) state instructions

module Main =

    [<Literal>]
    let FlagInterpret = "i"
    [<Literal>]
    let FlagCompile = "c"
    [<Literal>]
    let FlagExecute = "e" 

    [<EntryPoint>]
    let main argv =

        let args = Array.toList argv

        match args with
        | [] -> 0
        | flag::paths ->
            match flag with
            | FlagInterpret ->
                paths
                |> List.iter (fun arg ->
                    if File.Exists arg then
                        File.ReadAllLines arg
                        |> Array.toList
                        |> AssemblyParser.strip
                        |> AssemblyParser.parse
                        |> Stack.fold Stack.initialState
                        |> printfn "final stack: %A"
                )
                0
            | FlagCompile ->
                paths
                |> List.iter (fun arg ->
                    if File.Exists arg then

                        let path = 
                            let fullPath = Path.GetFullPath(arg)
                            fullPath.Split [|'/'; '\\'|]
                            |> Array.toList
                            |> List.rev
                            |> List.tail
                            |> List.rev
                            |> List.fold (fun path partial -> 
                                match path with
                                | "" -> partial
                                | _ -> sprintf "%s/%s" path partial) ""

                        let fileName =
                            let fullName =
                                arg.Split [|'/'; '\\'|]
                                |> Array.toList
                                |> List.rev
                                |> List.head
                            fullName.Split [|'.'|]
                            |> Array.toList
                            |> List.head
                        let target = sprintf "%s/%s.%s" path fileName "svm"

                        let binaryResult =
                            File.ReadAllLines arg
                            |> Array.toList
                            |> AssemblyParser.strip
                            |> AssemblyParser.parse
                            |> Compiler.serializeSasm

                        if not <| File.Exists target then
                            let fs = File.Create target
                            fs.Close()                            
                        File.WriteAllBytes(target, binaryResult)
                        )
                0
            | FlagExecute ->
                paths
                |> List.iter (fun arg ->
                    if File.Exists arg then
                        File.ReadAllBytes arg
                        |> Compiler.deserializeSasm
                        |> Stack.fold Stack.initialState
                        |> printfn "final stack: %A")
                0
            | _ -> 1

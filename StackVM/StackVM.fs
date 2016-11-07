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
        | Multiply
        | Print
        | Ignore
        | Halt
        | Copy

module AssemblyParser = 
    type OrElseBuilder() = 
        member this.ReturnFrom x = x
        
        member this.Combine(a, b) = 
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
    let getMultiplyLine = simpleLineHelper "mul" Multiply
    let getDivisionLine = simpleLineHelper "div" Divide
    let getPopLine = simpleLineHelper "pop" Pop
    let getHaltLine = simpleLineHelper "halt" Halt
    let getCopyLine = simpleLineHelper "copy" Copy
    
    let getPushLine input = 
        if isNull input then None
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
        |> List.filter (fun line -> not <| line.StartsWith("#"))
        //Filter empty lines
        |> List.filter (fun line -> line <> "")
        //Trim all lines
        |> List.map (fun line -> line.Trim())
    
    let parse (lines : string list) = 
        lines |> List.map (fun line -> 
                     let statement = 
                         orElse { 
                             return! getPushLine line
                             return! getAddLine line
                             return! getSubtractLine line
                             return! getDivisionLine line
                             return! getMultiplyLine line
                             return! getPrintLine line
                             return! getPopLine line
                             return! getHaltLine line
                             return! getCopyLine line
                         }
                     match statement with
                     | Some x -> x
                     | None -> Ignore)

module Compiler = 
    let serializeBinary<'a> (x : 'a) = 
        let binFormatter = new BinaryFormatter()
        use stream = new MemoryStream()
        binFormatter.Serialize(stream, x)
        stream.ToArray()
    
    let deserializeBinary<'a> (arr : byte []) = 
        let binFormatter = new BinaryFormatter()
        use stream = new MemoryStream(arr)
        binFormatter.Deserialize(stream) :?> 'a
    
    let serializeSasm = serializeBinary<Instruction list>
    let deserializeSasm = deserializeBinary<Instruction list>
    let stripIgnores = List.filter (fun instruction -> instruction <> Ignore)

module Stack = 
    let initialState = []
    let printDebug str =
#if DEBUG 
        printfn "[StackVM]: %s" str
#else
        ()
#endif
        
    
    let calcNewState currentState instruction = 
        let dbgIdentifier = "[StackVM]:"
        match instruction with
        | Push x -> 
            // Push x on top of the stack
            printDebug <| sprintf "pushing %i on the stack" x
            x :: currentState
        | Pop -> 
            // Pop the top of the stack
            printDebug "popping from the stack"
            List.tail currentState
        | Add -> 
            match currentState with
            | x :: y :: rst -> 
                printDebug <| sprintf "adding %i and %i" x y
                (y + x) :: rst
            | _ -> 
                printDebug 
                    "something went horribly wrong with the stack while trying to add"
                currentState
        | Subtract -> 
            match currentState with
            | x :: y :: rst -> 
                printDebug <| sprintf "subtracting %i and %i" x y
                (y - x) :: rst
            | _ -> 
                printDebug 
                    "something went horribly wrong with the stack while trying to subtract"
                currentState
        | Divide -> 
            match currentState with
            | x :: y :: rst -> 
                printDebug <| sprintf "dividing %i and %i" x y
                (y / x) :: rst
            | _ -> 
                printDebug 
                    "something went horribly wrong with the stack while trying to divide"
                currentState
        | Multiply -> 
            match currentState with
            | x :: y :: rst -> 
                printDebug <| sprintf "multiplying %i and %i" x y
                (y * x) :: rst
            | _ -> 
                printDebug 
                    "something went horribly wrong with the stack while trying to Multiply"
                currentState
        | Print -> 
            printDebug "printing the current head"
            printfn "%i" <| List.head currentState
            currentState
        | Halt -> 
            printDebug "halt detected"
            currentState
        | Copy -> 
            match currentState with
            | x :: tail -> 
                printDebug <| sprintf "copying %i" x
                x :: x :: tail
            | _ -> 
                printDebug 
                    "something went horribly wrong with the stack while trying to copy"
                currentState
        | Ignore -> 
            printDebug "i do not know that instruction"
            currentState
    
    let fold state instructions = 
        let rec _fold stack instructions = 
            match instructions with
            | instruction :: tail -> 
                match instruction with
                | Halt -> stack
                | _ -> 
                    let newstate = calcNewState stack instruction
                    _fold newstate tail
            | [] -> stack
        _fold state instructions

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
        | flag :: paths -> 
            match flag with
            | FlagInterpret -> 
                paths |> List.iter (fun arg -> 
                             if File.Exists arg then 
                                 File.ReadAllLines arg
                                 |> Array.toList
                                 |> AssemblyParser.strip
                                 |> AssemblyParser.parse
                                 |> Stack.fold Stack.initialState
                                 |> printfn "final stack: %A")
                0
            | FlagCompile -> 
                paths |> List.iter (fun arg -> 
                             if File.Exists arg then 
                                 let path = 
                                     let fullPath = Path.GetFullPath(arg)
                                     fullPath.Split [| '/'; '\\' |]
                                     |> Array.toList
                                     |> List.rev
                                     |> List.tail
                                     |> List.rev
                                     |> List.fold (fun path partial -> 
                                            match path with
                                            | "" -> partial
                                            | _ -> sprintf "%s/%s" path partial) 
                                            ""
                                 
                                 let fileName = 
                                     let fullName = 
                                         arg.Split [| '/'; '\\' |]
                                         |> Array.toList
                                         |> List.rev
                                         |> List.head
                                     fullName.Split [| '.' |]
                                     |> Array.toList
                                     |> List.head
                                 
                                 let target = 
                                     sprintf "%s/%s.%s" path fileName "svm"
                                 
                                 let binaryResult = 
                                     File.ReadAllLines arg
                                     |> Array.toList
                                     |> AssemblyParser.strip
                                     |> AssemblyParser.parse
                                     |> Compiler.stripIgnores
                                     |> Compiler.serializeSasm
                                 if not <| File.Exists target then 
                                     let fs = File.Create target
                                     fs.Close()
                                 File.WriteAllBytes(target, binaryResult))
                0
            | FlagExecute -> 
                paths |> List.iter (fun arg -> 
                             if File.Exists arg then 
                                 File.ReadAllBytes arg
                                 |> Compiler.deserializeSasm
                                 |> Stack.fold Stack.initialState
                                 |> printfn "final stack: %A")
                0
            | _ -> 1

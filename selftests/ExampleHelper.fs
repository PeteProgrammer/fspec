﻿module FSpec.SelfTests.ExampleHelper
open FSpec.Core
open Matchers
open Dsl
open MatchersV3

// Example building helpers
let pass = fun _ -> ()
let fail = fun _ -> raise (AssertionError { Message = "failed" })

let anExampleNamed name = Example.create name pass
let anExampleWithCode = Example.create "dummy"
let aPassingExample = anExampleWithCode pass
let aFailingExample = anExampleWithCode fail
let aPendingExample = anExampleWithCode pending
let anExample = aPassingExample
let anExceptionThrowingExample = anExampleWithCode (fun _ -> raise (new System.Exception()))

let withExampleMetaData md = TestDataMap.create [md] |> Example.addMetaData
let anExampleWithMetaData data = aPassingExample |> withExampleMetaData data

// Example group building helpers
let anExampleGroupNamed = ExampleGroup.create
let anExampleGroup = anExampleGroupNamed "dummy"

let withMetaData data = TestDataMap.create [data] |> ExampleGroup.addMetaData
let withSetupCode = ExampleGroup.addSetup
let withTearDownCode = ExampleGroup.addTearDown

let applyNestedContext f grp = grp |> f |> ExampleGroup.addChildGroup
let withNestedGroupNamed name f = anExampleGroupNamed name |> applyNestedContext f
let withNestedGroup f = anExampleGroup |> applyNestedContext f

let withExamples examples exampleGroup =
    let folder grp ex = ExampleGroup.addExample ex grp
    examples |> List.fold folder exampleGroup

let withAnExampleWithMetaData metaData =
    anExample
    |> withExampleMetaData metaData
    |> ExampleGroup.addExample

let withExampleCode f = anExampleWithCode f |> ExampleGroup.addExample
let withAnExampleNamed name = anExampleNamed name |> ExampleGroup.addExample
let withAnExample = anExample |> ExampleGroup.addExample

// Run helper
let run exampleGroup = 
    let reporter = Helpers.TestReporter.instance
    Runner.doRun exampleGroup reporter (reporter.BeginTestRun())

// ---- Custom matchers ----

let haveMetaData k v =
    let f a =
        let x =
            a |> ExampleGroup.getMetaData |> TestDataMap.tryGet k
        match x with
        | Some y -> y = v
        | None -> false
    createSimpleMatcher f

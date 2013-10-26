module SelfTests
open FSpec
open Expectations

let pass () = ()
let fail () = failwithf "Test failure"

type DummyType = {Name: string}

let specs =
    describe "TestCollection" <| fun() ->
        it "handles lazy initialization" <| fun () ->
            let c = TestCollection()
            let initCount = ref 0
            c.describe "Ctx" <| fun () ->
                let value = c.init <| fun () ->
                    initCount := !initCount + 1
                    "dummy"

                c.it "uses value" <| fun () ->
                    let x = value()
                    ()

                c.it "uses value twice" <| fun () ->
                    let x = value()
                    let y = value()
                    ()

            c.run()
            !initCount |> should equal 2

    describe "TestCollection" <| fun() ->
        let col = init (fun () -> TestCollection())
        let res = init (fun () -> TestReport())
        let run () =
            col().run(res())
            res()

        let _describe x = col().describe x
        let _it x = col().it x
        let _before x = col().before x
        let _after x = col().after x

        describe "Execution order" <| fun() ->
            let order = ref []
            let functionCalled x = order := x::!order
            let actualOrder () = !order |> List.rev

            before <| fun () -> order := []

            describe "after" <| fun() ->
                it "runs after the test is run" <| fun() ->
                    _after <| fun() ->
                        functionCalled "tearDown"
                    _it "dummy" <| fun() ->
                        functionCalled "test"
                    run() |> ignore
                    actualOrder() |> should equal ["test"; "tearDown"]

                it "runs if test fail" <| fun() ->
                    _after <| fun() ->
                        functionCalled "tearDown"
                    _it "fails" <| fun() ->
                        failwith "some failure"
                    run() |> ignore
                    actualOrder() |> should equal ["tearDown"]

                it "runs inner teardowns before outer teardowns" <| fun() ->
                    _describe "outer ctx" <| fun() ->
                        _after <| (fun() -> functionCalled "outer tearDown")
                        _describe "inner ctx" <| fun() ->
                            _after <| (fun() -> functionCalled "inner tearDown")
                            _it "dummy" <| (fun() -> functionCalled "test")
                    run() |> ignore
                    let expected = ["test"; "inner tearDown"; "outer tearDown"]
                    actualOrder() |> should equal expected

                it "runs teardown in the right context" <| fun() ->
                    _describe "outer ctx" <| fun() ->
                        _after <| (fun() -> functionCalled "outer tearDown")
                        _it "outer test" <| (fun() -> functionCalled "outer test")
                        _describe "inner ctx" <| fun() ->
                            _after <| (fun() -> functionCalled "inner tearDown")
                            _it "inner text" <| (fun() -> functionCalled "inner test")
                    run() |> ignore
                    let expected = ["outer test"; "outer tearDown"; "inner test"; "inner tearDown"; "outer tearDown"]
                    actualOrder() |> should equal expected


            describe "before" <| fun () ->
                it "runs before the test is run" <| fun () ->
                    _before <| fun() ->
                        functionCalled "before"
                    _it "dummy" <| fun() ->
                        functionCalled "test"
                    run() |> ignore
                    actualOrder() |> should equal ["before"; "test"]

                it "runs the inner setup before the outer setup" <| fun () ->
                    _describe "outer ctx" <| fun () ->
                        _before <| fun () -> 
                            functionCalled "outer setup"
                        _describe "inner ctx" <| fun() ->
                            _before <| fun () -> 
                                functionCalled "inner setup"
                            _it "has a test" pass
                    run() |> ignore
                    actualOrder() |> should equal ["outer setup";"inner setup"]

                it "is only run for in same context, or nested context" <| fun () ->
                    let outerSetupRunCount = ref 0
                    let innerSetupRunCount = ref 0
                    _describe "Ctx" <| fun () ->
                        _before <| fun () ->
                            outerSetupRunCount := !outerSetupRunCount + 1
                        _it "Outer test" pass
                        _describe "Inner ctx" <| fun () ->
                            _before <| fun () ->
                                innerSetupRunCount := !innerSetupRunCount + 1
                            _it "Inner test" pass
                            _it "Inner test2" pass
                    run() |> ignore
                    !innerSetupRunCount |> should equal 2
                    !outerSetupRunCount |> should equal 3

        describe "Run" <| fun () ->
            it "reports test failures" <| fun () ->
                _it "Is a failure" fail

                let report = run()
                report.summary() |> should equal "1 run, 1 failed"

            it "reports test success" <| fun() ->
                _it "Is a success" pass

                let report = run()
                report.summary() |> should equal  "1 run, 0 failed"

            it "runs the tests in the right order" <| fun() ->
                let no = ref 0
                let testNo () =
                    no := !no + 1
                    !no
                let test1No = ref 0
                let test2No = ref 0
                _describe("context") <| fun() ->
                    _it("has test 1") <| fun() ->
                        test1No := testNo()
                    _it("has test 2") <| fun() ->
                        test2No := testNo()

                run() |> ignore
                !test1No |> should equal 1
                !test2No |> should equal 2

            it "runs the contexts in the right order" <| fun() ->
                let no = ref 0
                let testNo () =
                    no := !no + 1
                    !no
                let test1No = ref 0
                let test2No = ref 0
                _describe("context") <| fun() ->
                    _it("has test 1") <| fun() ->
                        test1No := testNo()
                _describe("other context") <| fun() ->
                    _it("has test 2") <| fun() ->
                        test2No := testNo()

                run() |> ignore
                !test1No |> should equal 1
                !test2No |> should equal 2

        describe "Running status" <| fun () ->
            it "Is reported while running" <| fun () ->
                _describe "Some context" <| fun () ->
                    _it "has some behavior" pass
                let report = run()
                report.testOutput() |> should equal ["Some context has some behavior - passed"]

            it "Reports multiple test results" <| fun () ->
                _describe "Some context" <| fun() ->
                    _it "has some behavior" pass
                    _it "has some other behavior" pass

                let report = run()
                let actual = report.testOutput()
                let expected = ["Some context has some behavior - passed";
                                "Some context has some other behavior - passed"]
                actual.should equal expected

        describe "Failed tests" <| fun() ->
            it "Writes the output to the test report" <| fun() ->
                _it "Is a failing test" <| fun() ->
                    (5).should equal 6
                let result = run()
                let actual = result.failedTests() |> List.reduce (+)
                actual |> should matchRegex "expected 5 to equal 6"

            it "write the right output for comparison tests" <| fun() ->
                _it "Is a failing test" <| fun() ->
                    5 |> should be.greaterThan 6
                let result = run()
                let actual = result.failedTests() |> List.reduce (+)
                actual |> should matchRegex "expected 5 to be greater than 6"

            it "Is empty when no tests fail" <| fun() ->
                _it "Is a passing test" pass
                let result = run()
                let actual = result.failedTests() |> List.length
                actual.should equal 0

    describe "TestReport" <| fun() ->
        describe "With no failures reported" <| fun () ->
            it "Is a success" <| fun () ->
                let r = TestReport()
                r.success() |> should equal true

        describe "With failures reported" <| fun() ->
            it "Is a failure" <| fun () ->
                let r = TestReport()
                r.reportFailure()
                r.success() |> should equal false

    describe "Assertion helpers" <| fun() ->
        describe "equals" <| fun() ->
            it "passes when objects equal" <| fun() ->
                (5).should equal 5
            it "fails when the objects are not equal" <| fun() ->
                (fun () -> (5).should equal 6)
                    |> should throw ()

        describe "greaterThan" <| fun() ->
            it "passes when actual is greater than expected" <| fun() ->
                5 |> should be.greaterThan 4
            it "fails when actual is less than expected" <| fun() ->
                (fun () -> 5 |> should be.greaterThan 6)
                    |> should throw ()
            
            it "fails when actual is equal to expected" <| fun() ->
                (fun () -> 5 |> should be.greaterThan 5)
                    |> should throw ()

        describe "matches" <| fun() ->
            it "passes when the input matches the pattern" <| fun() ->
                "Some strange expression" |> should matchRegex "strange"

            it "fails when the input does not match the pattern" <| fun() ->
                (fun () -> "some value" |> should matchRegex "invalidPattern")
                    |> should throw ()
                
        describe "throw matcher" <| fun() ->
            let thrown = ref false
            let test (x : unit -> unit) =
                try
                    x |> should throw ()
                with
                    | _ -> thrown := true

            it "passed when an exception is thrown" <| fun () ->
                (fun () -> failwith "error") |> test
                !thrown |> should equal false
            it "fails when no exception is thrown" <| fun() ->
                (fun () -> ()) |> test
                !thrown |> should equal true

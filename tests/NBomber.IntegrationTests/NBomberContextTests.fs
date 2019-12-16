﻿module Tests.NBomberContextTests

open System

open Xunit
open Swensen.Unquote

open NBomber.Configuration
open NBomber.Contracts
open NBomber.DomainServices

open FsCheck
open FsCheck.Xunit

let globalSettings = { 
    ScenariosSettings = None
    TargetScenarios = Some ["1"]
    ReportFileName = None
    ReportFormats = None
}

let scenario = {
    ScenarioName = "1"
    TestInit = None
    TestClean = None
    Steps = Array.empty
    Assertions = Array.empty
    ConcurrentCopies = 1
    WarmUpDuration = TimeSpan.FromSeconds(10.)
    Duration = TimeSpan.FromSeconds(10.)
}

let config = {
    TestSuite = NBomber.Domain.Constants.DefaultTestSuite
    TestName = NBomber.Domain.Constants.DefaultTestName
    GlobalSettings = None
    ClusterSettings = None    
    CustomSettings = None
}

let context = {
    TestSuite = NBomber.Domain.Constants.DefaultTestSuite
    TestName = NBomber.Domain.Constants.DefaultTestName
    Scenarios = [| scenario |]
    NBomberConfig = None
    InfraConfig = None
    ReportFileName = None
    ReportFormats = []
    StatisticsSink = None
}

[<Fact>]
let ``NBomberContext.getTargetScenarios should return all registered scenarios if TargetScenarios are empty`` () =    
    let glSettings = { globalSettings with TargetScenarios = None }
    let config = { config with GlobalSettings = Some glSettings }
    let context = { context with NBomberConfig = Some config }
       
    match NBomberTestContext.getTargetScenarios(context) with
    | scenarios when scenarios.Length = 1 -> ()
    | _ -> failwith ""

[<Fact>]
let ``NBomberContext.getTargetScenarios should return only target scenarios if TargetScenarios are not empty`` () =    
    let glSettings = { globalSettings with TargetScenarios = Some ["10"] }
    let config = { config with GlobalSettings = Some glSettings }
    
    let scn1 = { scenario with ScenarioName = "1" }
    let scn2 = { scenario with ScenarioName = "2" }    
    
    let context = { context with NBomberConfig = Some config
                                 Scenarios = [| scn1; scn2 |] }
       
    match NBomberTestContext.getTargetScenarios(context) with
    | scenarios when scenarios.Length = 1 && scenarios.[0] = "10" -> ()
    | _ -> failwith ""

[<Property>]
let ``NBomberContext.getReportFileName should return from GlobalSettings, if empty then from NBomberContext, if empty then default name`` (configValue: string option, contextValue: string option) =

    (configValue.IsNone || configValue.IsSome && not (isNull configValue.Value)) ==> lazy
    (contextValue.IsNone || contextValue.IsSome && not (isNull contextValue.Value)) ==> lazy

    let glSettings = { globalSettings with ReportFileName = configValue }
    let config = { config with GlobalSettings = Some glSettings }

    let ctx = { context with NBomberConfig = Some config
                             ReportFormats = [ReportFormat.Txt]
                             ReportFileName = contextValue }
    
    let fileName = NBomberTestContext.getReportFileName("sessionId", ctx)

    match configValue, contextValue with
    | Some v1, Some v2 -> Assert.True(fileName.Equals v1)
    | Some v1, None -> Assert.True(fileName.Equals v1)
    | None, Some v2 -> Assert.True(fileName.Equals v2)
    | None, None    -> Assert.True(fileName.Equals "report_sessionId")

[<Property>]
let ``NBomberContext.getReportFormats should return from GlobalSettings, if empty then from NBomberContext, if empty then all supported formats`` (configValue: ReportFormat list option, contextValue: ReportFormat list) =

    let glSettings = { globalSettings with ReportFormats = configValue }
    let config = { config with GlobalSettings = Some glSettings }

    let ctx = { context with NBomberConfig = Some config
                             ReportFormats = contextValue
                             ReportFileName = None }
    
    let formats = NBomberTestContext.getReportFormats(ctx)

    match configValue, contextValue with
    | Some v, _ -> Assert.True((formats = v))
    
    | None, v when List.isEmpty v -> 
        Assert.True((formats = NBomber.Domain.Constants.AllReportFormats))
    
    | None, v -> Assert.True((formats = contextValue))
    
[<Property>]
let ``NBomberContext.getTestSuite should return from Config, if empty then from NBomberContext`` (configValue: string option, contextValue: string) =

    match configValue with
    | Some value ->
        let config = { config with TestSuite = value }
        let ctx = { context with NBomberConfig = Some config }    
        let testSuite = NBomberTestContext.getTestSuite(ctx)
        test <@ testSuite = value  @>
    
    | None ->
        let testSuite = NBomberTestContext.getTestSuite(context)
        test <@ testSuite = context.TestSuite @>
        
[<Property>]
let ``NBomberContext.getTestName should return from Config, if empty then from NBomberContext`` (configValue: string option, contextValue: string) =

    match configValue with
    | Some value ->
        let config = { config with TestName = value }
        let ctx = { context with NBomberConfig = Some config }    
        let testSuite = NBomberTestContext.getTestName(ctx)
        test <@ testSuite = value  @>
    
    | None ->
        let testSuite = NBomberTestContext.getTestName(context)
        test <@ testSuite = context.TestName @>
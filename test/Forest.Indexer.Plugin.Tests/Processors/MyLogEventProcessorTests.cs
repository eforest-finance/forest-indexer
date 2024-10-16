using AeFinder.Sdk;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.GraphQL;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace Forest.Indexer.Plugin.Processors;

public class MyLogEventProcessorTests: ForestIndexerPluginTestBase
{
    // private readonly MyLogEventProcessor _myLogEventProcessor;
    // private readonly IReadOnlyRepository<MyEntity> _repository;
    // private readonly IObjectMapper _objectMapper;
    //
    // public MyLogEventProcessorTests()
    // {
    //     _myLogEventProcessor = GetRequiredService<MyLogEventProcessor>();
    //     _repository = GetRequiredService<IReadOnlyRepository<MyEntity>>();
    //     _objectMapper = GetRequiredService<IObjectMapper>();
    // }
    //
    // [Fact]
    // public async Task Test()
    // {
    //     var logEvent = new MyLogEvent
    //     {
    //     };
    //     var logEventContext = GenerateLogEventContext(logEvent);
    //     await _myLogEventProcessor.ProcessAsync(logEvent, logEventContext);
    //     
    //     var entities = await Query.MyEntity(_repository, _objectMapper, new GetMyEntityInput
    //     {
    //         ChainId = ChainId
    //     });
    //     entities.Count.ShouldBe(1);
    // }
}
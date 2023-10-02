module CacheTests

open Dijon.Bot.Tests
open Dijon.Bot.Cache
open Dijon.Database
open Dijon.Database.StreamAnnouncements

open Faqt
open Faqt.Operators
open LazyCache
open Microsoft.Extensions.Caching.Memory
open Moq
open System
open System.Threading.Tasks
open Xunit

type CacheTests() =
    let guildId = GuildId 12345L
    let guildIdKey = $"StreamAnnouncementsChannel:Guild:{guildId.AsInt64}"

    let moqAppCache = Mock<IAppCache>(MockBehavior.Strict).SetupAllProperties()
    let databaseMock = Mock<IStreamAnnouncementsDatabase>(MockBehavior.Strict).SetupAllProperties()

    let service: IStreamCache = StreamCache(
        moqAppCache.Object,
        databaseMock.Object
    )

    [<Fact>]
    let ``FormatStreamAnnouncementChannelKey returns a string with the guild id`` () =
        let expectedKey = "StreamAnnouncementsChannel:Guild:12345"
        let actualKey = service.FormatStreamAnnouncementChannelKey guildId
        Assert.Equal(expectedKey, actualKey)

    [<Fact>]
    let ``LoadStreamDataForGuild returns stream data from the cache`` () = task {
        let expectedStreamData: StreamAnnouncementChannel = {
            Id = 2
            GuildId = 3
            ChannelId = 5
            StreamerRoleId = 7
        }

        moqAppCache.Setup(fun x -> x.GetOrAddAsync<StreamAnnouncementChannel option>(guildIdKey, It.IsAny<Func<ICacheEntry, Task<StreamAnnouncementChannel option>>>()))
            .ReturnsAsync(Some expectedStreamData)
            .Verifiable()

        let! actualStreamData = service.LoadStreamDataForGuild(guildId)

        %actualStreamData.Should()
            .BeSome()
            .That
            .Should()
            .Be(expectedStreamData)
    }

    [<Fact>]
    let ``LoadStreamDataForGuild loads stream data from the database when it it cannot be found in the cache`` () = task {
        let expectedStreamData: StreamAnnouncementChannel = {
            Id = 2
            GuildId = 3
            ChannelId = 5
            StreamerRoleId = 7
        }

        moqAppCache.Setup(fun x -> x.GetOrAddAsync<StreamAnnouncementChannel option>(guildIdKey, It.IsAny<Func<ICacheEntry, Task<StreamAnnouncementChannel option>>>()))
            .ReturnsAsync(Some expectedStreamData)
            .Verifiable()

        let act () = service.LoadStreamDataForGuild(guildId)

        %act.Should()
            .NotThrowAsync()
            .WhoseValue
            .Should()
            .Be(Some expectedStreamData)
    }

    [<Fact>]
    let ``ReleaseStreamDataForGuild removes the cached stream data for the guild`` () =
        moqAppCache.Setup(fun x -> x.Remove(guildIdKey))
            .Verifiable(Times.Once)

        let act _ = service.ReleaseStreamDataForGuild guildId

        %act.Should()
             .NotThrow()

        Mock.Verify(moqAppCache)

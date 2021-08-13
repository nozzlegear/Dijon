namespace Dijon

open Discord

module CommandParser =
    let (|ContainsSlander|_|) (a: string) = 
        let tdeDownWithDjurChannel = 561289801890791425L
        let slanderMessages = 
            [ "#downwithdjur" 
              "down with djur"
              ":downwithdjur:"
              sprintf "<#%i>" tdeDownWithDjurChannel ]

        if String.containsAny a slanderMessages then
            Some ContainsSlander 
        else
            None

    let (|AsksWhereFoxyIs|_|) (a: string) = 
        let whereIsFoxyMessages = 
            [ "where is foxy"
              "where's foxy"
              "wheres foxy"
              "donde esta foxy"
              "is foxy in dalaran"
              "is foxy in dal"
              "is foxy in moonglade"
              "where's foxy at"
              "can't find foxy"
              "cant find foxy" ]

        if String.containsAny a whereIsFoxyMessages then 
            Some AsksWhereFoxyIs
        else 
            None 

    let (|Mentioned|NotMentioned|) (msg: IMessage) = 
        //let mentionString = 
        //    bot.GetBotUserId ()
        //    |> MessageUtils.mentionUser 

        if String.startsWithAny msg.Content ["!dijon"; (*mentionString*)]
        then Mentioned
        else NotMentioned        

    let ParseCommand (msg: IMessage): Command = 
        match msg with 
        | NotMentioned -> 
            match msg.Content with 
            | ContainsSlander -> 
                Slander 
            | AsksWhereFoxyIs -> 
                FoxyLocation
            | _ -> 
                Ignore
        | Mentioned ->
            match Messages.SanitizeForParsing msg with 
            | "goulash"
            | "goulash recipe"
            | "scrapple"
            | "scrapple recipe"
            | "recipe" -> Goulash
            | "test" 
            | "test user left" -> TestUserLeft
            | "test stream started" -> TestStreamStarted
            | "test stream ended" -> TestStreamEnded
            | "status" -> Status
            | "set logs"
            | "log here"
            | "logs here"
            | "set logs here" 
            | "set log channel"
            | "set log channel here"
            | "set channel" -> SetLogChannel
            | "set affixes here"
            | "set affixes"
            | "affixes here"
            | "set affix channel here"
            | "affix here"
            | "set affix channel"
            | "set affixes channel" -> SetAffixesChannel
            | "set stream channel"
            | "set streams channel"
            | "set stream announcement channel"
            | "set stream announcements channel"
            | "set streams here"
            | "set stream announcements channel" 
            | "set stream announcements channel"
            | "set stream announcements channel to"
            | "set streams"
            | "set streams to"
            | "set streams for"
            | "set streams in"
            | "set streams in for"
            | "set streams for in"
            | "announce streams for"
            | "announce streams for in"
            | "announce streams" -> SetStreamsChannel
            | "remove stream channel"
            | "remove streams channel"
            | "unset stream channel"
            | "unset streams channel" -> UnsetStreamsChannel
            | "affix"
            | "what are the affixes"
            | "affixes" -> GetAffix
            | "help"
            | "tutorial"
            | "commands"
            | "command" -> Help
            | "hype"
            | "hype me up"
            | "hype squad" 
            | "tell them how it is"
            | "tell em how it is"
            | "tell em"
            | "tell them" -> Hype
            | "help me out"
            | "help me out here"
            | "back me up"
            | "this is outrageous"
            | "this is ridiculous"
            | "do something"
            | "do something!"
            | "pls"
            | "please"
            | "back me up" -> AidAgainstSlander
            | ContainsSlander -> Slander 
            | AsksWhereFoxyIs -> FoxyLocation
            | _ -> Unknown

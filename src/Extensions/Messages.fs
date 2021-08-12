namespace Dijon

open Discord

module Messages =
    /// Sanitizes the message's content for parsing into a command. Removes all mentions, strips the first word, transforms to lowercase and trims whitespace.
    let SanitizeForParsing (msg : IMessage) =
        let contentWithoutMentions =
            let removeMention (content : string) mention =
                // Mentions have three formats: 
                // <@&...> for roles
                // <@!...> for users
                // <#...> for channels

                content.Replace(sprintf "<@&%s>" mention, "")
                       .Replace(sprintf "<@!%s>" mention, "")
                       .Replace(sprintf "<#%s>"  mention, "")
            
            // Combine all of the mentions and remove them all from the content
            List.ofSeq msg.MentionedRoleIds 
            @ List.ofSeq msg.MentionedUserIds
            @ List.ofSeq msg.MentionedChannelIds
            |> List.map string
            |> List.fold removeMention msg.Content

        contentWithoutMentions
        |> String.stripFirstWord 
        |> String.lower 
        |> String.trim 
        |> String.trimBetweenWords

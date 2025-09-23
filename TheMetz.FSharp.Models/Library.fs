namespace TheMetz.FSharp

open Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models

module Models =
    type Link = { Title: string; Url: string }

    type ReviewCounts = { TotalReviews: int; ReviewsWithComments: int }

    type WorkItemInfo = {DeveloperName: string; mutable WorkItems: WorkItem seq}
namespace TheMetz.FSharp

module Models =
    type Link = { Title: string; Url: string }

    type ReviewCounts = { TotalReviews: int; WithComments: int }
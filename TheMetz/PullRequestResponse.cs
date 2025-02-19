using System;
using System.Collections.Generic;

namespace PullRequestModels
{
    public class PullRequestResponse
    {
        public List<PullRequest> Value { get; set; }
        public int Count { get; set; }
    }

    public class PullRequest
    {
        public Repository Repository { get; set; }
        public int PullRequestId { get; set; }
        public int CodeReviewId { get; set; }
        public string Status { get; set; }
        public CreatedBy CreatedBy { get; set; }
        public DateTime CreationDate { get; set; }
        public string Title { get; set; }
        public string SourceRefName { get; set; }
        public string TargetRefName { get; set; }
        public string MergeStatus { get; set; }
        public bool IsDraft { get; set; }
        public string MergeId { get; set; }
        public Commit LastMergeSourceCommit { get; set; }
        public Commit LastMergeTargetCommit { get; set; }
        public Commit LastMergeCommit { get; set; }
        public List<Reviewer> Reviewers { get; set; }
        public string Url { get; set; }
        public bool SupportsIterations { get; set; }
    }

    public class Repository
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public Project Project { get; set; }
    }

    public class Project
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string State { get; set; }
        public string Visibility { get; set; }
        public string LastUpdateTime { get; set; }
    }

    public class CreatedBy
    {
        public string DisplayName { get; set; }
        public string Url { get; set; }
        public Links Links { get; set; }
        public string Id { get; set; }
        public string UniqueName { get; set; }
        public string ImageUrl { get; set; }
        public string Descriptor { get; set; }
    }

    public class Links
    {
        public Avatar Avatar { get; set; }
    }

    public class Avatar
    {
        public string Href { get; set; }
    }

    public class Commit
    {
        public string CommitId { get; set; }
        public string Url { get; set; }
    }

    public class Reviewer
    {
        public string ReviewerUrl { get; set; }
        public int Vote { get; set; }
        public bool HasDeclined { get; set; }
        public bool IsRequired { get; set; }
        public bool IsFlagged { get; set; }
        public string DisplayName { get; set; }
        public string Url { get; set; }
        public Links Links { get; set; }
        public string Id { get; set; }
        public string UniqueName { get; set; }
        public string ImageUrl { get; set; }
        public bool IsContainer { get; set; }
        public List<VotedFor> VotedFor { get; set; }
    }

    public class VotedFor
    {
        public string ReviewerUrl { get; set; }
        public int Vote { get; set; }
        public string DisplayName { get; set; }
        public string Url { get; set; }
        public Links Links { get; set; }
        public string Id { get; set; }
        public string UniqueName { get; set; }
        public string ImageUrl { get; set; }
        public bool IsContainer { get; set; }
    }
}
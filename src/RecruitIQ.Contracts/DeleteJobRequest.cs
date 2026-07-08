using System;

namespace RecruitIQ.Contracts;

public record DeleteJobRequest(byte[] RowVersion);

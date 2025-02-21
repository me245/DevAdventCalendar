﻿using AutoMapper;
using DevAdventCalendarCompetition.Repository.Models;
using DevAdventCalendarCompetition.Services.Models;

namespace DevAdventCalendarCompetition.Services.Profiles
{
    public class TestProfile : Profile
    {
        public TestProfile()
        {
            CreateMap<Test, TestDto>()
                .ForMember(dest => dest.Answer, opt => opt.MapFrom(src => src.HashedAnswer));
            CreateMap<TestDto, Test>()
                .ForMember(dest => dest.Answers, opt => opt.Ignore())
                .ForMember(dest => dest.HashedAnswer, opt => opt.Ignore());
            CreateMap<Test, TestWithAnswerListDto>()
                .ForMember(dest => dest.TestId, opt => opt.Ignore());
        }
    }
}
﻿using DevAdventCalendarCompetition.Repository.Context;
using DevAdventCalendarCompetition.Repository.Interfaces;
using DevAdventCalendarCompetition.Repository.Models;
using System.Linq;

namespace DevAdventCalendarCompetition.Repository
{
    public class BaseTestRepository : IBaseTestRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public BaseTestRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Test GetByNumber(int testNumber)
        {
            return _dbContext.Set<Test>().FirstOrDefault(el => el.Number == testNumber);
        }

        public void AddTest(Test test)
        {
            _dbContext.Set<Test>().Add(test);
            _dbContext.SaveChanges();
        }

        public void AddAnswer(TestAnswer testAnswer)
        {
            _dbContext.Set<TestAnswer>().Add(testAnswer);
            _dbContext.SaveChanges();
        }

        public void AddWrongAnswer(TestWrongAnswer wrongAnswer)
        {
            _dbContext.Set<TestWrongAnswer>().Add(wrongAnswer);
            _dbContext.SaveChanges();
        }

        public void UpdateAnswer(TestAnswer testAnswer)
        {
            _dbContext.Set<TestAnswer>().Update(testAnswer);
            _dbContext.SaveChanges();
        }

        public TestAnswer GetAnswerByTestId(int testId)
        {
            return _dbContext.Set<TestAnswer>().FirstOrDefault(el => el.TestId == testId);
        }

        public bool HasUserAnsweredTest(string userId, int testId)
        {
            return _dbContext.Set<TestAnswer>().FirstOrDefault(el => el.TestId == testId && el.UserId == userId) != null;
        }
    }
}
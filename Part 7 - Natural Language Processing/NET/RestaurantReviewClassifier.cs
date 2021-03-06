﻿extern alias AccordText;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Accord.MachineLearning;
using Accord.MachineLearning.Bayes;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Univariate;
using AccordText::Accord.MachineLearning.Text.Stemmers;


namespace LanguageProcessing {
    public class RestaurantReviewClassifier {
        private BagOfWords _bagOfWordsModel;
        private NaiveBayes<NormalDistribution> _naiveBayesModel;
        private string[] _stopwords = File.ReadAllLines("stopwords.txt");
        private EnglishStemmer _englishStemmer = new EnglishStemmer();

        public RestaurantReviewClassifier(IEnumerable<RestaurantReview> trainingDataset) {
            this.Train(trainingDataset);
        }

        public bool PredictIsPositiveReview(string review) {
            review = this.CleanReview(review);
            string[] reviewWords = review.Split(' ');
            double[] bagOfWordsResult = this._bagOfWordsModel.Transform(reviewWords);
            int output = this._naiveBayesModel.Transform(bagOfWordsResult);

            return output == 1;
        }

        private string CleanReview(string review) {
            // Remove any non alphanumeric characters and convert to lower
            review = review.RemoveAllNonLetters().ToLower();

            // Remove all english stopwords
            review = String.Join(" ", review.Split(' ').Where(word => !this._stopwords.Contains(word)));

            // Stem every word
            review = String.Join(" ", review.Split(' ').Select(word => this._englishStemmer.Stem(word)));

            return review;
        }

        private void Train(IEnumerable<RestaurantReview> trainingDataset) {
            // Our independant variable of the review text
            string[] inputs = trainingDataset.Select(x => x.Review).ToArray();
            
            // Our dependant variable is whether or not the review is positive
            int[] outputs = trainingDataset.Select(x => Convert.ToInt32(x.IsPositive)).ToArray();

            // Clean review text
            inputs = inputs.Select(this.CleanReview).ToArray(); 
            
            // Convert the reviews into a multidimensial array. Each review will contain the words of of the review
            // Also removes any punctation and other marks
            string[][] wordsPerReview = inputs.Tokenize();

            // Use the bag of words model to creates a sparse matrix that will say wether or not a review contains a certain word
            // All words will be added a column
            this._bagOfWordsModel = new BagOfWords();
            this._bagOfWordsModel.Learn(wordsPerReview);
            double[][] bagOfWordsResult = this._bagOfWordsModel.Transform(wordsPerReview);

            // Use the naive bayes algorithm for our text classification. 
            NaiveBayesLearning<NormalDistribution> naiveBayesTeacher = new NaiveBayesLearning<NormalDistribution>();
            naiveBayesTeacher.Options.InnerOption = new NormalOptions() {
                Regularization = 1e-5 // to avoid zero variances exceptions
            };
            this._naiveBayesModel = naiveBayesTeacher.Learn(bagOfWordsResult, outputs);
        }
    }
}

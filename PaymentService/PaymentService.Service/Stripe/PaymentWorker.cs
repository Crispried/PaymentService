﻿using PaymentService.Service.Abstract;
using PaymentService.Service.Entities;
using PaymentService.Service.ViewModels.Request.CardVM;
using PaymentService.Service.ViewModels.Request.CustomerVM;
using PaymentService.Service.ViewModels.Request.PaymentVM;
using Stripe;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaymentService.Service.Stripe
{
    public class PaymentWorker : StripeWorker, IPaymentWorker
    {
        private readonly ICustomerWorker _customerWorker;

        public PaymentWorker(ICustomerWorker customerWorker)
        {
            _customerWorker = customerWorker;
        }

        public async Task MakeRegularPayment(RegularPaymentsRequestVM payment)
        {
            var chargeService = new StripeChargeService();

            await chargeService.CreateAsync(new StripeChargeCreateOptions()
            {
                CustomerId = payment.CustomerId,
                Currency = payment.Currency,
                Amount = payment.Amount
            });
        }

        public async Task MakeOneTimePayment(OneTimePaymentRequestVM payment)
        {
            var card = new CardRequestVM()
            {
                Number = payment.Card.Number,
                CVC = payment.Card.CVC,
                ExpirationMonth = payment.Card.ExpirationMonth,
                ExpirationYear = payment.Card.ExpirationYear
            };

            var createCustomerResponseVM = await _customerWorker.Create(new CreateCustomerRequestVM()
            {
                Card = card,
                Email = payment.Email,
                Name = payment.Name
            });

            await MakeRegularPayment(new RegularPaymentsRequestVM()
            {
                Amount = payment.Amount,
                Currency = payment.Currency,
                CustomerId = createCustomerResponseVM.CustomerId
            });

            await _customerWorker.Delete(createCustomerResponseVM.CustomerId);
        }

        public async Task<IEnumerable<Payment>> GetCustomerPayments(string customerId)
        {
            var chargeService = new StripeChargeService();

            var stripeCharges = await chargeService.ListAsync(new StripeChargeListOptions()
            {
                CustomerId = customerId
            });

            var customerPayments = new List<Payment>();
            foreach (var charge in stripeCharges.Data)
            {
                customerPayments.Add(new Payment()
                {
                    Amount = charge.Amount,
                    Currency = charge.Currency
                });
            }

            return customerPayments;
        }
    }
}
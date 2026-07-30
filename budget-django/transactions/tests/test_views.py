from unittest.mock import patch, MagicMock

from django.contrib.auth import get_user_model
from django.core.files.uploadedfile import SimpleUploadedFile
from django.test import RequestFactory, TestCase

from transactions.views import TransactionUploadView

# class HomeView(TemplateView):
#     template_name = "myapp/home.html"
#
#     def get_context_data(self, **kwargs):
#         kwargs["environment"] = "Production"
#         return super().get_context_data(**kwargs)

User = get_user_model()

class UploadFileViewTestCase(TestCase):

    @patch('transactions.models.Transaction.objects.process_file')
    def test_post__when_valid_file__calls_manager_and_returns_success(self, mock_process_file: MagicMock):
        # arrange
        mock_process_file.return_value = 10

        request = RequestFactory().post("/")
        request.FILES['file'] = SimpleUploadedFile('transaction.csv', None , 'text/csv')
        request.user = User.objects.create_user('test', '', '')
        view = TransactionUploadView()
        view.setup(request)

        # act
        result = view.post(request)

        # assert
        self.assertContains(result, '10 transacties verwerkt')

    @patch('transactions.models.Transaction.objects.process_file')
    def test_post__when_missing_file__returns_missing_file_message(self, mock_process_file: MagicMock):
        # arrange
        mock_process_file.return_value = 0

        request = RequestFactory().post("/")
        request.user = User.objects.create_user('test', '', '')
        view = TransactionUploadView()
        view.setup(request)

        # act
        result = view.post(request)

        # assert
        self.assertContains(result, 'Geen bestand toegevoegd')

    @patch('transactions.models.Transaction.objects.process_file')
    def test_post__when_process_file_fails__returns_unknown_error_message(self, mock_process_file: MagicMock):
        # arrange
        mock_process_file.side_effect = Exception('Kablam!')

        request = RequestFactory().post("/")
        request.user = User.objects.create_user('test', '', '')
        request.FILES['file'] = SimpleUploadedFile('transaction.csv', None , 'text/csv')
        view = TransactionUploadView()
        view.setup(request)

        # act
        result = view.post(request)

        # assert
        self.assertContains(result, 'Er is iets overwachts misgegaan')

#!/usr/bin/python3

import uuid
import boto3
from botocore.client import Config


BUCKET = {'Name': '<bucket_name>'} # <--- заменить
FILENAME = 'sample.txt'

def main():
    # Создание клиента
    s3 = boto3.client(
        's3',
        endpoint_url='https://s3.timeweb.com',
        region_name='ru-1',
        aws_access_key_id='<account_name>', # <--- заменить
        aws_secret_access_key='<secret_key>', # <--- заменить
        config=Config(s3={'addressing_style': 'path'})
    )

    print('Создание бакета')
    s3.create_bucket(Bucket=BUCKET['Name'])

    print()
    print('Метаданные бакета')
    try:
        result = s3.head_bucket(Bucket=BUCKET['Name'])
        print(result.get('Metadata', {}))
    except Exception as err:
        print(f'Ошибка при получении данных о бакете: {err}')

    print()
    print('Регион бакета')
    print(s3.get_bucket_location(Bucket=BUCKET['Name']).get('LocationConstraint'))

    print()
    print('Список бакетов')
    for bucket in s3.list_buckets().get('Buckets', []):
        print(bucket['Name'])

    print()
    print('Создание файла из скрипта')
    s3.put_object(Bucket=BUCKET['Name'], Key='new_file', Body='test_body')

    print()
    print('Загрузка файла в бакет')
    s3.upload_file(Filename=FILENAME, Bucket=BUCKET['Name'], Key='sample.txt')

    print()
    print('Список объектов в бакете')
    for obj in s3.list_objects(Bucket=BUCKET['Name']).get('Contents', []):
        print(obj['Key'])

    print()
    print('Метаданные объекта')
    print(s3.head_object(Bucket=BUCKET['Name'], Key='sample.txt').get('Metadata'))

    print()
    print('Копирование объекта')
    print(s3.copy_object(CopySource={'Bucket': BUCKET['Name'], 'Key': 'new_file'}, Bucket=BUCKET['Name'], Key='copy_object'))

    print()
    print('Чтение файла')
    data = s3.get_object(Bucket=BUCKET['Name'], Key='new_file').get('Body')
    if data is not None:
        print(data.read())

    print()
    print('Удаление объектов')
    for obj in ['sample.txt', 'new_file', 'copy_object']:
        try:
            s3.delete_object(Bucket=BUCKET['Name'], Key=obj)
            print(f'Объект {obj} удален')
        except Exception as err:
            print(f'Не удалось удалить объект {obj}: {err}')

    print()
    print('Удаление бакета')
    s3.delete_bucket(Bucket=BUCKET['Name'])

    return s3


if __name__ == '__main__':
    s3 = main()
